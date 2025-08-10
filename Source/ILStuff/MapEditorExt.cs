

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Editor;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monocle;
using MonoMod.Cil;

namespace Celeste.Mod.FCHelper.ILStuff;

class MapEditorExt
{
  private static List<Vector2> hearts;
  private static List<Vector2> cassettes;
  private static List<Vector2> moonBerries;
  private static int currentMB = 0;
  private static int currentHeart = 0;

  public static void Render(Action<MapEditor> orig, MapEditor self)
  {
    orig(self);

    hearts = new List<Vector2>();
    cassettes = new List<Vector2>();
    moonBerries = new List<Vector2>();

    foreach (LevelData level in self.mapData.Levels)
    {
      Rectangle bounds = level.Bounds;
      Vector2 vector = new Vector2(bounds.X, bounds.Y);
      foreach (EntityData item in Enumerable.Where(level.Entities, (EntityData entityData) => entityData.Name == "blackGem"))
      {
        hearts.Add((vector + item.Position) / 8f);
      }
    }

    foreach (LevelData level in self.mapData.Levels)
    {
      Rectangle bounds = level.Bounds;
      Vector2 vector = new Vector2(bounds.X, bounds.Y);
      foreach (EntityData item in Enumerable.Where(level.Entities, (EntityData entityData) => entityData.Name == "cassette"))
      {
        cassettes.Add((vector + item.Position) / 8f);
      }
    }


    foreach (LevelData level in self.mapData.Levels)
    {
      Rectangle bounds = level.Bounds;
      Vector2 vector = new Vector2(bounds.X, bounds.Y);
      level.Entities.ForEach(item =>
      {
        try
        {
          if (item.Values.TryGetValue("moon", out object value))
          {
            if (value is bool moon && moon)
            {
              moonBerries.Add((vector + item.Position) / 8f);
            }
          }

        }
        catch (Exception) { }
      });
    }

    DrawObjects(hearts, "H", Color.Blue, Keys.F1);
    DrawObjects(cassettes, "C", Color.White, Keys.F1);
    DrawObjects(moonBerries, "MB", Color.Cyan, Keys.F1);
  }

  static void DrawObjects(List<Vector2> items, string text, Color color, Keys keys)
  {
    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, MapEditor.Camera.Matrix * Engine.ScreenMatrix);
    foreach (var item in items)
    {
      Draw.HollowRect(item.X - 1f, item.Y - 2f, 3f, 3f, color);
    }
    Draw.SpriteBatch.End();

    if (MInput.Keyboard.Check(keys))
    {
      Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Engine.ScreenMatrix);
      foreach (var item in items)
      {
        ActiveFont.DrawOutline(text, (item - MapEditor.Camera.Position + Vector2.UnitX) * MapEditor.Camera.Zoom + new Vector2(960f, 540f), new Vector2(0.5f, 0.5f), Vector2.One * 1f, color, 2f, Color.Black);
      }
      Draw.SpriteBatch.End();
    }
  }

  public static void IL_RenderManuelText(ILContext il)
  {
    ILCursor cursor = new ILCursor(il);

    if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchStloc(0)))
      return;

    if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchStloc(0)))
      return;

    cursor.EmitDelegate(ReplaceManuelText);
  }

  public static string ReplaceManuelText(string text)
  {
    return "Right Click:  Teleport to the room\nConfirm:      Teleport to the room\nHold Control: Restart Chapter before teleporting\nHold Shift:   Teleport to the mouse position\nCancel:       Exit debug map\nQ:            Show berries\nF1:           Show other collectables\nF2:           Center on current respawn point\nF3:           Center on cassette\nF4:           Center on moon berry\nF5:           Show/Hide instructions\nF6:           Center on heart";
  }

  public static void Update(Action<MapEditor> orig, MapEditor self)
  {
    orig(self);

    if (MInput.Keyboard.Pressed(Keys.F3))
    {
      if (cassettes.Count == 0) return;

      MapEditor.Camera.position = cassettes[0];
    }

    if (MInput.Keyboard.Pressed(Keys.F4))
    {
      if (moonBerries.Count == 0) return;

      currentMB++;
      if (currentMB >= moonBerries.Count)
      {
        currentMB = 0;
      }

      MapEditor.Camera.position = moonBerries[currentMB];
    }

    if (MInput.Keyboard.Pressed(Keys.F6))
    {
      if (hearts.Count == 0) return;

      currentHeart++;
      if (currentHeart >= hearts.Count)
      {
        currentHeart = 0;
      }

      MapEditor.Camera.position = hearts[currentHeart];
    }
  }
}
