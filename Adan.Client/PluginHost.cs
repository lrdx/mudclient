﻿namespace Adan.Client
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using System.Windows;

    using Common.Conveyor;
    using Common.Model;
    using Common.Plugins;

    using CSLib.Net.Annotations;
    using CSLib.Net.Diagnostics;
    using Common.ViewModel;
    using Common.Utils;

    /// <summary>
    /// Class to host plugins.
    /// </summary>
    public sealed class PluginHost : IDisposable
    {
        private static PluginHost _instance;

        private readonly AggregateCatalog _catalog;
        private readonly CompositionContainer _container;

        private string _currentOutputWindow = string.Empty;

        /// <summary>
        /// Prevents a default instance of the <see cref="PluginHost"/> class from being created.
        /// </summary>
        private PluginHost()
        {
            AllPlugins = new List<PluginBase>();
            Plugins = new List<PluginBase>();

            try
            {
                _catalog = new AggregateCatalog();
                _catalog.Catalogs.Add(new DirectoryCatalog("Plugins"));

                _container = new CompositionContainer(_catalog);
                _container.ComposeParts(this);
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Gets the default instance of this class.
        /// </summary>
        [NotNull]
        public static PluginHost Instance
        {
            get
            {
                return _instance ?? (_instance = new PluginHost());
            }
        }

        /// <summary>
        /// Gets the all loaded plugins.
        /// </summary>
        [NotNull]
        [ImportMany(typeof(PluginBase))]
        public IList<PluginBase> AllPlugins
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the all loaded plugins.
        /// </summary>
        [NotNull]
        public IList<PluginBase> Plugins
        {
            get;
            private set;
        }

        private List<WidgetDescription> _widgets;
        /// <summary>
        /// Gets all widgets.
        /// </summary>
        [NotNull]
        public IEnumerable<WidgetDescription> Widgets
        {
            get
            {
                if (_widgets == null)
                {
                    _widgets = new List<WidgetDescription>();
                    foreach (var plugin in Plugins)
                    {
                        try
                        {
                            _widgets.AddRange(plugin.Widgets);
                        }
                        catch (Exception ex)
                        {
                            ErrorLogger.Instance.Write(string.Format("Error initialize widgets for plugin {0}: {1}\r\n{2}", plugin.Name, ex.Message, ex.StackTrace));
                        }
                    }
                }

                return _widgets;
            }
        }
        
        public void OutputWindowCreated(RootModel rootModel)
        {
            foreach (var plugin in Plugins)
            {
                try
                {
                    plugin.OnCreatedOutputWindow(rootModel);
                }
                catch (Exception ex)
                {
                    ErrorLogger.Instance.Write(string.Format("Plugin error {0}: {1}\r\n{2}", plugin.Name, ex.Message, ex.StackTrace));
                }
            }
        }
        
        public void OutputWindowChanged(RootModel rootModel)
        {
            if (rootModel.Uid != _currentOutputWindow)
            {
                foreach (var plugin in Plugins)
                {
                    try
                    {
                        plugin.OnChangedOutputWindow(rootModel);
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger.Instance.Write(string.Format("Plugin error {0}: {1}\r\n{2}", plugin.Name, ex.Message, ex.StackTrace));
                    }
                }

                _currentOutputWindow = rootModel.Uid;
            }
        }
        
        public void OutputWindowClose(RootModel rootModel)
        {
            foreach (var plugin in Plugins)
            {
                try
                {
                    plugin.OnClosedOutputWindow(rootModel);
                }
                catch (Exception ex)
                {
                    ErrorLogger.Instance.Write(string.Format("Plugin error {0}: {1}\r\n{2}", plugin.Name, ex.Message, ex.StackTrace));
                }
            }
        }

        /// <summary>
        /// Applies the additional plugin merge dictionaries.
        /// </summary>
        public void ApplyAdditionalPluginMergeDictionaries()
        {
            foreach (var plugin in Plugins)
            {
                try
                {
                    foreach (var resourceUrl in plugin.PluginXamlResourcesToMerge)
                    {
                        Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(resourceUrl, UriKind.Relative) });
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.Instance.Write(string.Format("Plugin error {0}: {1}\r\n{2}", plugin.Name, ex.Message, ex.StackTrace));
                }
            }
        }


        public void InitializePlugins([NotNull] InitializationStatusModel initializationStatusModel, [NotNull] Window mainWindow)
        {
            Assert.ArgumentNotNull(initializationStatusModel, "initializationStatusModel");
            Assert.ArgumentNotNull(mainWindow, "MainWindowEx");

            foreach (var plugin in AllPlugins)
            {
                try
                {
                    plugin.Initialize(initializationStatusModel, mainWindow);
                    Plugins.Add(plugin);

                    foreach (var actionDescription in plugin.CustomActions)
                    {
                        RootModel.AllActionDescriptions.Add(actionDescription);
                    }

                    foreach (var actionParameter in plugin.CustomActionParameters)
                    {
                        RootModel.AllParameterDescriptions.Add(actionParameter);
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger.Instance.Write(string.Format("Plugin initialize error {0}: {1}\r\n{2}", plugin.Name, ex.Message, ex.StackTrace));
                }
            }

            ApplyAdditionalPluginMergeDictionaries();
        }

        public void InitializeConveyor(MessageConveyor conveyor)
        {
            foreach (var plugin in AllPlugins)
            {
                plugin.InitializeConveyor(conveyor);
            }
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                foreach (var plugin in Plugins)
                    plugin.Dispose();

                if (_container != null)
                {
                    _container.Dispose();
                }

                if (_catalog != null)
                {
                    _catalog.Dispose();
                }
            }
            catch (Exception)
            { }
        }
    }
}
