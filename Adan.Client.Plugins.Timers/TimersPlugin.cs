using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adan.Client.Plugins.Timers
{
    using Common.Plugins;
    using System.ComponentModel.Composition;
    using Common.Conveyor;
    using System.Windows;
    using Adan.Client.Common.ViewModel;
    using CSLib.Net.Diagnostics;
    using System.Text.RegularExpressions;
    using Adan.Client.Plugins.Timers.ConveyorUnits;
    using Adan.Client.Common.Model;
    using CSLib.Net.Annotations;

    /// <summary>
    /// A <see cref="PluginBase"/> implementation to display ticks.
    /// </summary>
    [Export(typeof(PluginBase))]
    public sealed class TimersPlugin : PluginBase
    {
        public override string Name => "Timers";

        public override void Initialize(InitializationStatusModel initializationStatusModel, Window MainWindowEx)
        {
            Assert.ArgumentNotNull(initializationStatusModel, "initializationStatusModel");

            initializationStatusModel.CurrentPluginName = "Timers";
            initializationStatusModel.PluginInitializationStatus = "Initializing";
        }

        public override void InitializeConveyor(MessageConveyor conveyor)
        {
            conveyor.AddConveyorUnit(new TimerConveyorUnit(conveyor));
        }
    }
}
