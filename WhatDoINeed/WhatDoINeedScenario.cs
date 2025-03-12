using System;
using System.Collections.Generic;
using KSP_Log;


namespace WhatDoINeed
{

    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] { GameScenes.EDITOR })]
    public  class WhatDoINeedScenario:ScenarioModule
    {

        public override void OnLoad(ConfigNode node)
        {
            Settings.Instance.LoadData(node);

        }
        public override void OnSave(ConfigNode node)
        {
            Settings.Instance.SaveData(node);
        }
    }
}
