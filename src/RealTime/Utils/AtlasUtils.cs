namespace RealTime.Utils
{
    using UnityEngine;

    public static class AtlasUtils
    {
        public static string[] LockButtonSpriteNames =
        [
            "UnLock",
            "Lock"
        ];

        public static string[] EditButtonSpriteNames =
        [
            "Edit",
            "NoneEdit"
        ];

        public static string[] CopyPasteSpriteNames =
        [
            "Copy",
            "Paste"
        ];

        public static void CreateAtlas()
        {
            if (TextureUtils.GetAtlas("LockButtonAtlas") == null)
            {
                TextureUtils.InitialiseAtlas("LockButtonAtlas");
                TextureUtils.AddSpriteToAtlas(new Rect(8, 0, 27, 32), LockButtonSpriteNames[0], "LockButtonAtlas");
                TextureUtils.AddSpriteToAtlas(new Rect(36, 0, 20, 32), LockButtonSpriteNames[1], "LockButtonAtlas");
            }

            if (TextureUtils.GetAtlas("ClearScheduleButton") == null)
            {
                TextureUtils.InitialiseAtlas("ClearScheduleButton");
                TextureUtils.AddSpriteToAtlas(new Rect(1, 1, 36, 32), "ClearSchedule", "ClearScheduleButton");
            }

            if (TextureUtils.GetAtlas("EditButtonAtlas") == null)
            {
                TextureUtils.InitialiseAtlas("EditButtonAtlas");
                TextureUtils.AddSpriteToAtlas(new Rect(6, 4, 23, 24), EditButtonSpriteNames[0], "EditButtonAtlas");
                TextureUtils.AddSpriteToAtlas(new Rect(35, 4, 23, 24), EditButtonSpriteNames[1], "EditButtonAtlas");
            }

            if (TextureUtils.GetAtlas("CopyPasteAtlas") == null)
            {
                TextureUtils.InitialiseAtlas("CopyPasteAtlas");
                TextureUtils.AddSpriteToAtlas(new Rect(0, 0, 64, 64), CopyPasteSpriteNames[0], "CopyPasteAtlas");
                TextureUtils.AddSpriteToAtlas(new Rect(64, 0, 64, 64), CopyPasteSpriteNames[1], "CopyPasteAtlas");
            }
        }
    }
}
