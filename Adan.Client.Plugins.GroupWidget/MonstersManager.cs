﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Adan.Client.Common.Model;
using Adan.Client.Plugins.GroupWidget.Model;
using Adan.Client.Plugins.GroupWidget.ViewModel;
using CSLib.Net.Annotations;
using CSLib.Net.Diagnostics;

namespace Adan.Client.Plugins.GroupWidget
{
    /// <summary>
    /// 
    /// </summary>
    public class MonstersManager
    {
        private readonly MonstersWidgetControl _monstersWidget;
        private readonly Dictionary<string, MonsterHolder> _monsterHolders;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="monstersWidget"></param>
        public MonstersManager([NotNull] MonstersWidgetControl monstersWidget)
        {
            Assert.ArgumentNotNull(monstersWidget, "monstersWidget");

            _monstersWidget = monstersWidget;
            _monsterHolders = new Dictionary<string, MonsterHolder>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootModel"></param>
        public void OutputWindowCreated([NotNull] RootModel rootModel)
        {
            Assert.ArgumentNotNull(rootModel, "rootModel");

            _monsterHolders.Add(rootModel.Uid, new MonsterHolder(this, rootModel));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uid"></param>
        public void OutputWindowClosed([NotNull] string uid)
        {
            Assert.ArgumentNotNullOrWhiteSpace(uid, "uid");

            if (_monsterHolders.ContainsKey(uid))
                _monsterHolders.Remove(uid);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uid"></param>
        public void OutputWindowChanged([NotNull] string uid)
        {
            Assert.ArgumentNotNullOrWhiteSpace(uid, "uid");

            if (_monsterHolders.ContainsKey(uid))
            {
                var monstersViewModel = _monstersWidget.DataContext as RoomMonstersViewModel;
                if (monstersViewModel.RootModel != null)
                {
                    monstersViewModel.RootModel.SelectedRoomMonster = null;
                }
                monstersViewModel.RootModel = _monsterHolders[uid].RootModel;
                _monstersWidget.ViewModelUid = uid;
                _monstersWidget.UpdateModel(_monsterHolders[uid].Characters);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="monsterHolder"></param>
        public void UpdateMonsters([NotNull] MonsterHolder monsterHolder)
        {
            Assert.ArgumentNotNull(monsterHolder, "monsterHolder");

            if (_monstersWidget.ViewModelUid == monsterHolder.Uid)
            {
                _monstersWidget.UpdateModel(monsterHolder.Characters);
            }
        }
    }
}
