
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.FCHelper.ILStuff;

class PauseMenuExt
{
  public static void IL_RenderExtraCollectibles(ILContext il)
  {
    ILCursor cursor = new ILCursor(il);

    // need to inject extra space for the cassette
    // just after loading num4
    if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdloc(7)))
      return;
    cursor.EmitDelegate(addCassetteWidth);

    // just before getInitialPosition(ref orig);
    if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCall<GameplayStats>("getInitialPosition")))
      return;
    // insert delegate to draw extra collectibles
    cursor.EmitDelegate(RenderExtraCollectibles);

    // go to where the position is getting updated in the outer loop
    if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdloc(5)))
      return;
    if (!cursor.TryGotoNext(MoveType.After, instr => instr.MatchLdloc(5)))
      return;
    // load the value of i
    cursor.EmitLdloc(10);
    cursor.EmitDelegate(offsetIfCassette);

    // just before the loop counter is incremented
    if (!cursor.TryGotoNext(MoveType.Before,
        instr => instr.MatchLdloc(10),
        instr => instr.MatchLdcI4(1),
        instr => instr.MatchAdd()
        ))
      return;
    // go after i is loaded
    if (!cursor.TryGotoNext())
      return;
    // load the value of orig
    cursor.EmitLdloc(8);
    // takes orig and i and returns i
    cursor.EmitDelegate(DrawCassette);

  }

  // add space for the cassette
  static int addCassetteWidth(int num4)
  {
    return num4 + 50;
  }

  // draw cassette
  // takes the loop counter that holds the checkpoint and adds padding to the position
  static int DrawCassette(int i, Vector2 orig)
  {
    Level level = Engine.Scene as Level;
    if (level == null)
      return i;

    AreaKey area = level.Session.Area;
    AreaStats areaStats = SaveData.Instance.Areas_Safe[area.ID];
    AreaModeStats areaModeStats = areaStats.Modes[(int)area.Mode];
    ModeProperties modeProperties = AreaData.Get(area).Mode[(int)area.Mode];
    AreaData areaData = AreaData.Get(area);

    int cassetteCheckpoint = modeProperties.MapMeta?.CassetteCheckpointIndex ?? areaData.CassetteCheckpointIndex;
    bool collectedCassette = level.Session.Cassette;

    if (i + 1 == cassetteCheckpoint)
    {
      // draw cassette
      MTexture mTexture = GFX.Gui["collectables/cassette"];
      orig.X -= 48;

      if (level.Session.Cassette)
      {
        mTexture.DrawOutlineCentered(orig, Calc.HexToColor("FFFFFF"), 0.2f);
      }
      else
      {
        if (areaStats.Cassette)
        {
          mTexture.DrawOutlineCentered(orig, Calc.HexToColor("4193ff"), 0.2f);
        }
        else
        {
          mTexture.DrawOutlineCentered(orig, Calc.HexToColor("777777"), 0.2f);
        }
      }
    }
    return i;
  }

  static int offsetIfCassette(int num2, int i)
  {
    Level level = Engine.Scene as Level;
    if (level == null)
      return num2;

    AreaKey area = level.Session.Area;
    ModeProperties modeProperties = AreaData.Get(area).Mode[(int)area.Mode];
    AreaData areaData = AreaData.Get(area);

    int cassetteCheckpoint = modeProperties.MapMeta?.CassetteCheckpointIndex ?? areaData.CassetteCheckpointIndex;
    bool collectedCassette = level.Session.Cassette;

    if (i + 1 == cassetteCheckpoint)
    {
      return num2 + 60;
    }
    return num2;
  }

  static ref Vector2 RenderExtraCollectibles(ref Vector2 orig)
  {
    Level level = Engine.Scene as Level;
    if (level == null)
      return ref orig;

    AreaKey area = level.Session.Area;
    AreaModeStats areaModeStats = SaveData.Instance.Areas_Safe[area.ID].Modes[(int)area.Mode];
    ModeProperties modeProperties = AreaData.Get(area).Mode[(int)area.Mode];
    AreaData areaData = AreaData.Get(area);

    List<EntityData> moonBerries = getMoonBerries(modeProperties);

    bool hasHeart = modeProperties.MapData.DetectedHeartGem;

    int cassetteCheckpoint = modeProperties.MapMeta?.CassetteCheckpointIndex ?? areaData.CassetteCheckpointIndex;
    bool collectedCassette = level.Session.Cassette;

    Vector2 position = new Vector2(1920 / 2 - (32 * moonBerries.Count * 0.5f), orig.Y - 45.0f);
    if (hasHeart)
    {
      position.X -= 40 / 2.0f;
    }

    // draw moon berries
    if (moonBerries.Count > 0)
    {
      MTexture mTexture = GFX.Gui["dot"];

      for (int i = 0; i < moonBerries.Count; i++)
      {
        EntityData moonBerry = moonBerries[i];

        bool collectedCurrent = false;
        foreach (EntityID strawberry in level.Session.Strawberries)
        {
          if (moonBerry.ID == strawberry.ID && moonBerry.Level.Name == strawberry.Level)
          {
            collectedCurrent = true;
          }
        }

        if (collectedCurrent)
        {
          mTexture.DrawOutlineCentered(position, Calc.HexToColor("00FFB9"), 1.0f);
        }
        else
        {
          bool collectedEver = false;
          foreach (EntityID strawberry2 in areaModeStats.Strawberries)
          {
            if (moonBerry.ID == strawberry2.ID && moonBerry.Level.Name == strawberry2.Level)
            {
              collectedEver = true;
            }
          }

          if (collectedEver)
          {
            mTexture.DrawOutlineCentered(position, Calc.HexToColor("FFFFFF"), 1.0f);
          }
          else
          {
            Draw.Rect(position.X - (float)mTexture.ClipRect.Width * 0.5f, position.Y - 4f, mTexture.ClipRect.Width, 8f, Color.DarkGray);
          }
        }

        position.X += 32;
      }
    }

    if (hasHeart)
    {
      position.X += 20;
      if (level.Session.HeartGem)
      {
        MTexture mTexture = GFX.Gui["collectables/heartgem/1/Spin00"];
        mTexture.DrawOutlineCentered(position, Calc.HexToColor("FFFFFF"), 0.2f);
      }
      else
      {
        if (areaModeStats.HeartGem)
        {
          MTexture mTexture = GFX.Gui["collectables/heartgem/0/Spin00"];
          mTexture.DrawOutlineCentered(position, Calc.HexToColor("FFFFFF"), 0.2f);
        }
        else
        {
          MTexture mTexture = GFX.Gui["collectables/heartgem/3/FakeHeart0000"];
          mTexture.DrawOutlineCentered(position, Calc.HexToColor("999999"), 0.2f);
        }
      }
    }

    return ref orig;
  }

  public static List<EntityData> getMoonBerries(ModeProperties modeProperties)
  {
    List<EntityData> moonBerries = new List<EntityData>();

    int checkpointCount = (modeProperties.Checkpoints == null) ? 1 : (modeProperties.Checkpoints.Length + 1);

    modeProperties.MapData.Strawberries.ForEach(strawberry =>
    {
      if (strawberry.Values.TryGetValue("moon", out object value))
      {
        if (value is bool moon && moon)
        {
          moonBerries.Add(strawberry);
        }
      }
    });

    return moonBerries;
  }
}
