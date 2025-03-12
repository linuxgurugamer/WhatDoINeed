using UnityEngine;
using static WhatDoINeed.RegisterToolbar;


namespace WhatDoINeed
{
    public partial class WhatDoINeed
    {
        const int WIDTH = 300;
        const int HEIGHT = 200;

        string IsArePlural(int x, bool plural = false)
        {
            if (!plural)
                return ((x == 1) ? "is " : "are ");
            return (x == 1) ? "" : "s";
        }

        string IsArePlural(int x, string str, string str2 = " ")
        {
            return "There " + IsArePlural(x) + x + str + IsArePlural(x, true) + str2;

        }

        public void OnLaunchButtonInput()
        {
            if (Settings.Instance.checkForMissingBeforeLaunch)
            {
                int numSelectedcontracts = 0;
                foreach (var contract in Repository.Contracts)
                {
                    if (contract.Value.selected || !Settings.Instance.onlyCheckSelectedContracts)
                    {
                        numSelectedcontracts++;
                    }
                }
                PreprocessContracts();

                if (numSelectedcontracts > 0 && numMissingExperiments > 0)
                {
                    // You have active contracts that requires some parts that your vessel currently does not have.Are you sure you want to launch?
                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f),
                                           new Vector2(0.5f, 0.5f),
                                           new MultiOptionDialog("Fill It Up",
                                               "You have active contracts that have experiments/requirements\n" +
                                               "that can't be run due to missing parts.\n\n" +

                                               IsArePlural(numSelectedcontracts, " selected contract") + "\n" +
                                               IsArePlural(numFillableContracts, " contract", " that can be fulfilled") + "\n\n" +
                                               htmlRed + IsArePlural(numUnfillableContracts, " contract", " that cannot be fulfilled") + "</color>" + "\n\n" +
                                               //htmlRed + IsArePlural(numMissingExperiments, " missing requirement") + "</color>" + "\n\n" +

                                               "Are you sure you want to launch?\n\n" +
                                               "Please select your option from the choices below",
                                               "Unfullfillable Contracts",
                                               HighLogic.UISkin,
                                               new Rect(0.5f, 0.5f, WIDTH, HEIGHT),
                                               new DialogGUIFlexibleSpace(),
                                               new DialogGUIVerticalLayout(
                                                   new DialogGUIFlexibleSpace(),

                                                   new DialogGUIHorizontalLayout(
                                                       new DialogGUIFlexibleSpace(),
                                                       new DialogGUIButton("OK to launch",
                                                           delegate
                                                           {
                                                               //ResetDelegates();
                                                               //Log.Info("OnLaunchButtonInput 1");
                                                               //defaultLaunchDelegate();
                                                               ButtonManager.BtnManager.InvokeNextDelegate(btnId, "What-Do-I-Need-next");

                                                           }, 240.0f, 30.0f, true),
                                                        new DialogGUIFlexibleSpace()
                                                    ),

                                                    new DialogGUIFlexibleSpace(),

                                                    new DialogGUIHorizontalLayout(
                                                       new DialogGUIFlexibleSpace(),
                                                       new DialogGUIButton("Cancel", () => { }, 240.0f, 30.0f, true),
                                                       new DialogGUIFlexibleSpace()
                                                       )
                                                   )
                                               ),
                                                false,
                                                HighLogic.UISkin);
                }
                else
                {
                    //Log.Info("OnLaunchButtonInput 4");
                    ButtonManager.BtnManager.InvokeNextDelegate(btnId, "What-Do-I-Need-next");
                }
            }
            else
            {
                //Log.Info("OnLaunchButtonInput 3");
                ButtonManager.BtnManager.InvokeNextDelegate(btnId, "What-Do-I-Need-next");
            }
        }

    }
}
