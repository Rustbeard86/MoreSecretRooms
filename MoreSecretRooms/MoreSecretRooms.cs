using System;
using System.Collections.Generic;
using Ricave.Core;
using UnityEngine;

namespace MoreSecretRooms
{
    public class SecretRoomEventsHandler : ModEventsHandler
    {
        public SecretRoomEventsHandler(Mod mod) : base(mod)
        {
        }

        public override void SubscribeToEvents(ModsEventsManager eventsManager)
        {
            eventsManager.Subscribe(ModEventType.WorldGenerated, OnWorldGenerated);
        }

        private static void OnWorldGenerated(object worldObj)
        {
            try
            {
                Log.Message("[MoreSecretRooms] WorldGenerated event fired.");

                var memory = WorldGen.CurMemory;

                if (memory.config.Spec != Get.World_Standard)
                {
                    Log.Message("[MoreSecretRooms] Skipping execution: Not a standard procedural world.");
                    return;
                }

                var roomsAdded = 0;
                for (var i = 0; i < 3; i++)
                    if (TryAddSideCorridorSecretRoom(memory))
                        roomsAdded++;

                Log.Message($"[MoreSecretRooms] Finished. Added {roomsAdded} new secret room(s).");
            }
            catch (Exception ex)
            {
                Log.Error("[MoreSecretRooms] CRITICAL ERROR during OnWorldGenerated.", ex);
            }
        }

        private static bool TryAddSideCorridorSecretRoom(WorldGenMemory memory)
        {
            var potentialRooms = new List<Room>();
            foreach (var room in memory.AllRooms)
                if (room.Shape.width > 7 || room.Shape.depth > 7)
                    potentialRooms.Add(room);

            if (potentialRooms.Count == 0) return false;

            if (!potentialRooms.TryGetRandomElement(out var chosenRoom)) return false;

            var roomShape = chosenRoom.Shape.CellRectXZ;
            Vector3Int entrancePos;
            CellCuboid newRoomShape;

            if (roomShape.width > roomShape.height) // Horizontal room
            {
                var x = roomShape.Center.x;
                var placeAbove = Rand.Bool;
                var z = placeAbove ? roomShape.yMax : roomShape.y - 5;
                entrancePos = new Vector3Int(x, 1, placeAbove ? roomShape.yMax - 1 : roomShape.y);
                newRoomShape = new CellCuboid(x - 2, 1, z, 5, 4, 5);
            }
            else // Vertical room
            {
                var z = roomShape.Center.y;
                var placeRight = Rand.Bool;
                var x = placeRight ? roomShape.xMax : roomShape.x - 5;
                entrancePos = new Vector3Int(placeRight ? roomShape.xMax - 1 : roomShape.x, 1, z);
                newRoomShape = new CellCuboid(x, 1, z - 2, 5, 4, 5);
            }

            foreach (var room in memory.AllRooms)
                if (room.Shape.Overlaps(newRoomShape))
                    return false;
            // Obstructed location

            Get.World.RetainedRoomInfo.Add(
                newRoomShape,
                Get.Specs.Get<RoomSpec>("MySecretRoom_Treasure"),
                Room.LayoutRole.Secret,
                "A Hidden Vault"
            );

            var originalWall = Get.World.GetFirstEntityOfSpecAt(entrancePos, Get.Entity_Wall);
            if (originalWall != null)
            {
                originalWall.DeSpawn();
                Maker.Make(Get.Entity_WallWithCompartment).Spawn(entrancePos);
            }

            return true; // Success!
        }
    }
}