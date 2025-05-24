using System;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Celeste.Editor;
using Celeste.Mod.FCHelper.ILStuff;

namespace Celeste.Mod.FCHelper;
// GameplayStats MapEditor
public class FCHelperModule : EverestModule
{
    public static FCHelperModule Instance { get; private set; }

    public override Type SettingsType => typeof(FCHelperModuleSettings);
    public static FCHelperModuleSettings Settings => (FCHelperModuleSettings)Instance._Settings;

    public override Type SessionType => typeof(FCHelperModuleSession);
    public static FCHelperModuleSession Session => (FCHelperModuleSession)Instance._Session;

    public override Type SaveDataType => typeof(FCHelperModuleSaveData);
    public static FCHelperModuleSaveData SaveData_ => (FCHelperModuleSaveData)Instance._SaveData;

    public static Hook mapEditorRender;
    public static Hook mapEditorUpdate;

    public FCHelperModule()
    {
        Instance = this;
#if DEBUG
        // debug builds use verbose logging
        Logger.SetLogLevel(nameof(FCHelperModule), LogLevel.Verbose);
#else
        // release builds use info logging to reduce spam in log files
        Logger.SetLogLevel(nameof(FCHelperModule), LogLevel.Info);
#endif
    }

    public override void Load()
    {
        IL.Celeste.GameplayStats.Render += PauseMenuExt.IL_RenderExtraCollectibles;

        mapEditorRender = new Hook(
                typeof(MapEditor).GetMethod("Render", BindingFlags.Public | BindingFlags.Instance),
                typeof(MapEditorExt).GetMethod("Render"));

        mapEditorUpdate = new Hook(
                typeof(MapEditor).GetMethod("Update", BindingFlags.Public | BindingFlags.Instance),
                typeof(MapEditorExt).GetMethod("Update"));


        IL.Celeste.Editor.MapEditor.RenderManualText += MapEditorExt.IL_RenderManuelText;

    }

    public override void Unload()
    {
        IL.Celeste.GameplayStats.Render -= PauseMenuExt.IL_RenderExtraCollectibles;

        mapEditorRender?.Dispose();
        mapEditorRender = null;

        IL.Celeste.Editor.MapEditor.RenderManualText -= MapEditorExt.IL_RenderManuelText;
    }
}
