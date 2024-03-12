using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CliWrap;
using GlazeWM.Domain.UserConfigs;
using GlazeWM.Domain.UserConfigs.Events;
using GlazeWM.Infrastructure;
using GlazeWM.Infrastructure.Bussing;
using Microsoft.Extensions.Logging;

namespace GlazeWM.Bar.Components;

public class ScriptedTextComponentViewModel : ComponentViewModel
{
  private readonly ScriptedTextConfig _baseConfig;
  private static readonly Regex Regex = new(@"\{[^}]+\}", RegexOptions.Compiled);
  private readonly ILogger<ScriptedTextComponent> _logger;
  private readonly Bus _bus;

  public string FormattedText { get; set; } = "Loading...";

  public ScriptedTextComponentViewModel(BarViewModel parentViewModel, ScriptedTextConfig baseConfig) : base(parentViewModel, baseConfig)
  {
    _baseConfig = baseConfig;
    _logger = ServiceLocator.GetRequiredService<ILogger<ScriptedTextComponent>>();
    _bus = ServiceLocator.GetRequiredService<Bus>();
    Init();
  }

  private void Init()
  {
    _ = RunScript();
    var updateInterval = TimeSpan.FromMilliseconds(_baseConfig.IntervalMs);
    Observable.Interval(updateInterval)
      .TakeUntil(_bus.Events.OfType<UserConfigReloadedEvent>())
      .Subscribe(_ => RunScript());
  }

  private async Task RunScript()
  {
    var errorBuffer = new StringBuilder(1000);
    try
    {
      var outSb = new StringBuilder(1000);
      var res = await Cli.Wrap(_baseConfig.ScriptPath)
        .WithArguments(_baseConfig.ScriptArgs)
        .WithStandardOutputPipe(PipeTarget.ToStringBuilder(outSb))
        .WithStandardErrorPipe(PipeTarget.ToStringBuilder(errorBuffer))
        .ExecuteAsync();
      var output = outSb.ToString();

      if (string.Equals(_baseConfig.OutputType, "json", StringComparison.OrdinalIgnoreCase))
      {
        var json = Regex.Matches(output).Last(); // extract json block
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json.Value);
        FormattedText = result.Aggregate(_baseConfig.Label, (state, item) => state.Replace($"{{{item.Key}}}", item.Value.ToString()));
      }
      else
      {
        FormattedText = output;
      }
      OnPropertyChanged(nameof(FormattedText));
    }
    catch (Exception e)
    {
      _logger.LogError(e, "Error while execute text script, {ErrorOut}", errorBuffer.ToString());
    }
  }
}
