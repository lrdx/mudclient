﻿namespace Adan.Client.Plugins.GroupWidget.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Windows.Threading;

    using Common.Model;
    using Common.ViewModel;

    using CSLib.Net.Annotations;
    using CSLib.Net.Diagnostics;

    using Properties;

    /// <summary>
    /// A view model for group status widget.
    /// </summary>
    public class GroupStatusViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _tickingTimer;
        private int _displayedAffectCount = Settings.Default.GroupWidgetDisplayAffectsCount;
        private bool _displayNumber = Settings.Default.GroupWidgetDisplayNumber;
        private IList<string> _displayedAffectNames = new List<string>(Settings.Default.GroupWidgetAffects);
        private GroupMateViewModel _selectedGroupMate;
        private bool _moreItemsAvailable;

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupStatusViewModel"/> class.
        /// </summary>
        public GroupStatusViewModel()
        {
            GroupMates = new ObservableCollection<GroupMateViewModel>();
            AffectsPanelWidth = (29 * _displayedAffectNames.Count) + 26 + 26 + 31 + 60 + 125;

            _tickingTimer = new DispatcherTimer(DispatcherPriority.Background);
            _tickingTimer.Interval = TimeSpan.FromSeconds(1);
            _tickingTimer.Tick += (o, e) => UpdateTimings();
            _tickingTimer.Start();

            MemTimeVisible = Settings.Default.GroupWidgetDisplayMemTime;

            AffectsPanelWidth = _displayedAffectCount * 23;
            Width = AffectsPanelWidth + 22 + 140 + 60 + 20 + 20 + 5 + 5 + 20 + 44 + 4;
            if (!DisplayNumber)
            {
                Width -= 22;
            }

            if (!MemTimeVisible)
            {
                Width -= 44;
            }

            DisplayedItemLimit = Settings.Default.GroupWidgetLimitOn ? Settings.Default.GroupWidgetLimit : 9999;

            if (DisplayedItemLimit < 1)
            {
                DisplayedItemLimit = 1;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public RootModel RootModel
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        public string Uid
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the  width of affects panel.
        /// </summary>
        public double AffectsPanelWidth
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the width of control displaying monsters.
        /// </summary>
        public double Width
        {
            get; private set;
        }

        public int DisplayedAffectCount
        {
            get { return _displayedAffectCount; }
            set
            {
                _displayedAffectCount = value;
                OnPropertyChanged("DisplayedAffectCount");
            }
        }

        public bool DisplayNumber
        {
            get { return _displayNumber; }
            set
            {
                _displayNumber = value;
                OnPropertyChanged("DisplayNumber");
            }
        }

        /// <summary>
        /// Gets or sets the selected group mate.
        /// </summary>
        /// <value>
        /// The selected group mate.
        /// </value>
        [CanBeNull]
        public GroupMateViewModel SelectedGroupMate
        {
            get
            {
                return _selectedGroupMate;
            }

            set
            {
                _selectedGroupMate = value;
                if (RootModel != null)
                    RootModel.SelectedGroupMate = value != null ? value.GroupMate : null;
                OnPropertyChanged("SelectedGroupMate");
            }
        }

        /// <summary>
        /// Gets the group mates of current player.
        /// </summary>
        [NotNull]
        public ObservableCollection<GroupMateViewModel> GroupMates
        {
            get;
            private set;
        }

        public bool MoreItemsAvailable
        {
            get { return _moreItemsAvailable; }
            set
            {
                if (_moreItemsAvailable != value)
                {
                    _moreItemsAvailable = value;
                    OnPropertyChanged("MoreItemsAvailable");
                }
            }
        }

        public int DisplayedItemLimit { get; set; }

        public bool MemTimeVisible { get; set; }

        /// <summary>
        /// Updates the model.
        /// </summary>
        /// <param name="groupMates">The group mates as they come from server.</param>
        public void UpdateModel([NotNull] IEnumerable<CharacterStatus> groupMates)
        {
            Assert.ArgumentNotNull(groupMates, "groupMates");

            int position = 0;
            bool moreItemsAvailable = false;

            foreach (var characterStatus in groupMates)
            {
                if (position >= DisplayedItemLimit)
                {
                    moreItemsAvailable = true;
                    break;
                }

                if (position < GroupMates.Count && GroupMates[position].Name == characterStatus.Name)
                {
                    GroupMates[position].UpdateFromModel(characterStatus, position + 1);
                    if (SelectedGroupMate != null && SelectedGroupMate == GroupMates[position])
                    {
                        RootModel.SelectedGroupMate = characterStatus;
                    }

                }
                else
                {
                    var affectsList = _displayedAffectNames.Select(af => Constants.AllAffects.First(a => a.Name == af));
                    GroupMates.Insert(position, new GroupMateViewModel(characterStatus, affectsList, position + 1, AffectsPanelWidth) { DisplayNumber = DisplayNumber, MemTimeVisibleSetting = MemTimeVisible });
                }

                position++;
            }

            for (int i = position; i < GroupMates.Count; i++)
            {
                if (SelectedGroupMate != null && SelectedGroupMate == GroupMates[position])
                {
                    SelectedGroupMate = null;
                }

                GroupMates.RemoveAt(position);
            }

            MoreItemsAvailable = moreItemsAvailable;
        }

        /// <summary>
        /// Reloads the displayed affects.
        /// </summary>
        public void ReloadDisplayedAffects()
        {
            GroupMates.Clear();
            _displayedAffectNames = new List<string>(Settings.Default.GroupWidgetAffects);
            DisplayNumber = Settings.Default.GroupWidgetDisplayNumber;
            DisplayedAffectCount = Settings.Default.GroupWidgetDisplayAffectsCount;
            AffectsPanelWidth = _displayedAffectCount * 23;
            Width = AffectsPanelWidth + 22 + 140 + 60 + 20 + 20 + 5 + 5 + 20 + 44 + 4;
            MemTimeVisible = Settings.Default.GroupWidgetDisplayMemTime;
            if (!DisplayNumber)
            {
                Width -= 22;
            }

            if (!MemTimeVisible)
            {
                Width -= 44;
            }

            DisplayedItemLimit = Settings.Default.GroupWidgetLimitOn ? Settings.Default.GroupWidgetLimit : 9999;

            if (DisplayedItemLimit < 1)
            {
                DisplayedItemLimit = 1;
            }
            MoreItemsAvailable = false;
            OnPropertyChanged("Width");
        }

        private void UpdateTimings()
        {
            var now = DateTime.Now;

            foreach (var groupMate in GroupMates)
            {
                groupMate.UpdateTimings(now);
            }
        }
    }
}

