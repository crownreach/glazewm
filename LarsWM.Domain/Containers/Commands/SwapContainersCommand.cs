﻿using LarsWM.Infrastructure.Bussing;

namespace LarsWM.Domain.Containers.Commands
{
  public class SwapContainersCommand : Command
  {
    public Container FirstContainer { get; }
    public Container SecondContainer { get; }

    public SwapContainersCommand(Container firstContainer, Container secondContainer)
    {
      FirstContainer = firstContainer;
      SecondContainer = secondContainer;
    }
  }
}
