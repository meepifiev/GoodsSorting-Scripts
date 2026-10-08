using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Editor.Building
{
    public class BuildingUiSpriteImporter
    {
        public const string UiSpritesFolder = BuildingAssetPaths.SpritesFolder + "/UI";

        public static readonly string[] SpriteNames =
        {
            "2D_Outgame_Building_MainScene_task_button",
            "2D_Outgame_Building_MainScene_task_button_hole",
            "2D_Outgame_Building_MainScene_exit_button",
            "2D_Outgame_Building_MainScene_exit_button_hole",
            "2D_Outgame_Building_MainScene_BuildAll_button",
            "2D_Default_resource_bar_Gem",
            "2D_Default_noti",
            "2D_Outgame_Building_NewDay",
            "2D_Outgame_Building_Popup_Task_board_9slice_height",
            "2D_Outgame_Building_Popup_Task_board_stroke",
            "2D_Outgame_Building_Popup_Task_gem_icon",
            "2D_Outgame_Building_Popup_Task_Day_icon",
            "2D_Outgame_Building_Popup_Task_title_Sparky",
            "2D_Outgame_Stocking Challenge_gift box5",
            "2D_Default_button_Disable_9slice_wide",
            "2D_Outgame_Building_Popup_OutofGems_icon",
            "2D_Outgame_Building_Customize_board_9slice",
            "2D_Outgame_Building_Customize_option1",
            "2D_Outgame_Building_Customize_option1_selected",
            "2D_Outgame_Building_Customize_option2",
            "2D_Outgame_Building_Customize_option2_selected",
            "2D_Outgame_Building_Customize_option3",
            "2D_Outgame_Building_Customize_option3_selected",
            "2D_Outgame_Building_Customize_agree_button",
            "2D_Outgame_Building_claim_reward_icon",
            "2D_Outgame_Building_coming_soon_art",
            "2D_Outgame_Building_Popup_BuildAll_board_9slice_height",
            "2D_Default_PopUp_headline_exit_button_5",
            "2D_Outgame_Building_MainScene_task_button_2",
            "2D_Outgame_Home_Building_button",
            "2D_Outgame_Home_Building_button_disable"
        };

        private readonly ReferenceExport _export;
        private readonly BuildingImportReport _report;

        public BuildingUiSpriteImporter(ReferenceExport export, BuildingImportReport report)
        {
            _export = export ?? throw new ArgumentNullException(nameof(export));
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        public Dictionary<string, Sprite> Import()
        {
            ReferenceSpriteImporter sprites = new ReferenceSpriteImporter(_export, _report);
            Dictionary<string, string> guidByName = new Dictionary<string, string>();

            foreach (string name in SpriteNames)
            {
                string guid = _export.ReadGuid(ReferenceExport.SpriteFolder + "/" + name + ".asset");

                if (guid == null)
                {
                    _report.Warn("UI sprite not found in reference: " + name);
                    continue;
                }

                guidByName[name] = guid;
                sprites.Request(guid, UiSpritesFolder, false);
            }

            sprites.ImportAll();

            Dictionary<string, Sprite> result = new Dictionary<string, Sprite>();

            foreach (KeyValuePair<string, string> pair in guidByName)
            {
                Sprite sprite = sprites.Resolve(pair.Value);

                if (sprite != null)
                {
                    result[pair.Key] = sprite;
                }
            }

            return result;
        }
    }
}
