﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using GisSharpBlog.NetTopologySuite.IO;

namespace RuneSharp.RunePackage
{
    public class ConfigParser
    {
        //Parses flo.dat from config.jag
        //Yup it's fixed
        public static FloorConfig[] ParseFloorConfig(byte[] floorData)
        {
            int floorCount;
            FloorConfig[] floors;
            BigEndianBinaryReader floorReader = new BigEndianBinaryReader(new MemoryStream(floorData));
            floorCount = floorReader.ReadUInt16();
            floors = new FloorConfig[floorCount];
            bool finishFloor = false;
            for (int i = 0; i < floorCount; i++)
            {
                floors[i] = new FloorConfig();
                finishFloor = false;
                do
                {
                    int type = floorReader.ReadByte();
                    switch (type)
                    {
                        case 0:
                            finishFloor = true;
                            break;
                        case 1:
                            floors[i].actualColor = toRGB(floorReader.ReadUInt24());
                            break;
                        case 2:
                            floors[i].texture = floorReader.ReadByte();
                            break;
                        case 3:
                            floors[i].unknown = true;
                            break;
                        case 5:
                            floors[i].occlude = false;
                            break;
                        case 6:
                            floors[i].floorName = floorReader.ReadString().TrimEnd('\n');
                            break;
                        case 7:
                            floors[i].mapColor = toRGB(floorReader.ReadUInt24());
                            break;
                        default:
                            Logger.Log("Unrecognized floor type: " + type, LogType.Error);
                            break;
                    }
                } while (!finishFloor);
                Logger.Log(floors[i].ToString(), LogType.Success);
            }
            return floors;
        }
        public static ItemConfig[] ParseItemConfig(byte[] data, byte[] index)
        {
            BigEndianBinaryReader dataReader = new BigEndianBinaryReader(new MemoryStream(data));
            BigEndianBinaryReader indexReader = new BigEndianBinaryReader(new MemoryStream(index));
            int totalItems = indexReader.ReadUInt16();
            int[] streamIndices = new int[totalItems];
            ItemConfig[] itemConfig = new ItemConfig[totalItems];
            int i = 2;
            for (int j = 0; j < totalItems; j++)
            {
                streamIndices[j] = i;
                i += indexReader.ReadUInt16();
            }
            for (int x = 0; x < totalItems; x++)
            {
                dataReader.BaseStream.Position = streamIndices[x];
                ItemConfig item = new ItemConfig();
                do
                {
                    int opCode = dataReader.ReadByte();
                    if (opCode == 0)
                        break;
                    if (opCode == 1)
                        item.modelID = dataReader.ReadUInt16();
                    else if (opCode == 2)
                        item.name = dataReader.ReadString().TrimEnd('\n');
                    else if (opCode == 3)
                        item.description = dataReader.ReadString().TrimEnd('\n');
                    else if (opCode == 4)
                        item.modelInvZoom = dataReader.ReadUInt16();
                    else if (opCode == 5)
                        item.modelInvRotationY = dataReader.ReadUInt16();
                    else if (opCode == 6)
                        item.modelInvRotationX = dataReader.ReadUInt16();
                    else if (opCode == 7)
                    {
                        item.modelInvPosOffsetX = dataReader.ReadUInt16();
                        if (item.modelInvPosOffsetX > 32767)
                            item.modelInvPosOffsetX -= 0x10000;
                    }
                    else if (opCode == 8)
                    {
                        item.modelInvPosOffsetY = dataReader.ReadUInt16();
                        if (item.modelInvPosOffsetY > 32767)
                            item.modelInvPosOffsetY -= 0x10000;
                    }
                    else if (opCode == 10)
                        dataReader.ReadUInt16();
                    else if (opCode == 11)
                        item.stackable = true;
                    else if (opCode == 12)
                        item.value = dataReader.ReadInt32();
                    else if (opCode == 16)
                        item.membersObject = true;
                    else if (opCode == 23)
                    {
                        item.maleWornModelID = dataReader.ReadUInt16();
                        item.maleYOffset = dataReader.ReadByte();
                    }
                    else if (opCode == 24)
                        item.maleArmsID = dataReader.ReadUInt16();
                    else if (opCode == 25)
                    {
                        item.femaleWornModelID = dataReader.ReadUInt16();
                        item.femaleYOffset = dataReader.ReadByte();
                    }
                    else if (opCode == 26)
                        item.femaleArmsID = dataReader.ReadUInt16();
                    else if (opCode >= 30 && opCode < 35)
                    {
                        if (item.groundActions == null)
                            item.groundActions = new string[5];
                        item.groundActions[opCode - 30] = dataReader.ReadString().TrimEnd('\n');
                        if (item.groundActions[opCode - 30].ToLower() == "hidden")
                            item.groundActions[opCode - 30] = null;
                    }
                    else if (opCode >= 35 && opCode < 40)
                    {
                        if (item.actions == null)
                            item.actions = new string[5];
                        item.actions[opCode - 35] = dataReader.ReadString().TrimEnd('\n');
                    }
                    else if (opCode == 40)
                    {
                        int colors = dataReader.ReadByte();
                        item.originalModelColors = new int[colors];
                        item.modifiedModelColors = new int[colors];
                        for (int colorPtr = 0; colorPtr < colors; colorPtr++)
                        {
                            item.originalModelColors[colorPtr] = dataReader.ReadUInt16();
                            item.modifiedModelColors[colorPtr] = dataReader.ReadUInt16();
                        }
                    }
                    else if (opCode == 78)
                        item.maleEmblem = dataReader.ReadUInt16();
                    else if (opCode == 79)
                        item.femaleEmblem = dataReader.ReadUInt16();
                    else if (opCode == 90)
                        item.maleDialog = dataReader.ReadUInt16();
                    else if (opCode == 91)
                        item.femaleDialog = dataReader.ReadUInt16();
                    else if (opCode == 92)
                        item.maleDialogHat = dataReader.ReadUInt16();
                    else if (opCode == 93)
                        item.femaleDialogHat = dataReader.ReadUInt16();
                    else if (opCode == 95)
                        item.diagonalRotation = dataReader.ReadUInt16();
                    else if (opCode == 97)
                        item.certID = dataReader.ReadUInt16();
                    else if (opCode == 98)
                        item.certTemplateID = dataReader.ReadUInt16();
                    else if (opCode >= 100 && opCode < 110)
                    {
                        if (item.stackIDs == null)
                        {
                            item.stackIDs = new int[10];
                            item.stackAmounts = new int[10];
                        }
                        item.stackIDs[opCode - 100] = dataReader.ReadUInt16();
                        item.stackAmounts[opCode - 100] = dataReader.ReadUInt16();
                    }
                    else if (opCode == 110)
                        item.modelSizeX = dataReader.ReadUInt16();
                    else if (opCode == 111)
                        item.modelSizeY = dataReader.ReadUInt16();
                    else if (opCode == 112)
                        item.modelSizeZ = dataReader.ReadUInt16();
                    else if (opCode == 113)
                        item.lightModifier = dataReader.ReadUInt16();
                    else if (opCode == 114)
                        item.shadowModifier = dataReader.ReadByte() * 5;
                    else if (opCode == 115)
                        item.team = dataReader.ReadByte();
                    else if (opCode == 116)
                        item.lendID = dataReader.ReadUInt16();
                    else if (opCode == 117)
                        item.lentItemID = dataReader.ReadUInt16();
                    else
                        Logger.Log("Unknown Item Opcode: " + opCode, LogType.Error);
                } while (true);
                itemConfig[x] = item;
            }
            return itemConfig;
        }
        public static NPCConfig[] ParseNPCConfig(byte[] data, byte[] index)
        {
            BigEndianBinaryReader dataReader = new BigEndianBinaryReader(new MemoryStream(data));
            BigEndianBinaryReader indexReader = new BigEndianBinaryReader(new MemoryStream(index));
            int totalNPCs = indexReader.ReadUInt16();
            NPCConfig[] NPCList = new NPCConfig[totalNPCs];
            int[] streamIndices = new int[totalNPCs];
            int offset = 2;
            for (int npcPtr = 0; npcPtr < totalNPCs; npcPtr++)
            {
                streamIndices[npcPtr] = offset;
                offset += indexReader.ReadUInt16();
            }
            for (int j = 0; j < totalNPCs; j++)
            {
                NPCConfig npc = new NPCConfig();
                dataReader.BaseStream.Position = streamIndices[j];
                do
                {
                    byte i = dataReader.ReadByte();
                    if (i == 0)
                        break;
                    else if (i == 1)
                    {
                        int modelCount = dataReader.ReadByte();
                        npc.npcModels = new int[modelCount];
                        for (int k = 0; k < modelCount; k++)
                            npc.npcModels[k] = dataReader.ReadUInt16();
                    }
                    else if (i == 2)
                        npc.name = dataReader.ReadString();
                    else if (i == 3)
                        npc.description = dataReader.ReadString();
                    else if (i == 12)
                        npc.boundDim = dataReader.ReadByte();
                    else if (i == 13)
                        npc.idleAnimation = dataReader.ReadUInt16();
                    else if (i == 14)
                        npc.walkAnimIndex = dataReader.ReadUInt16();
                    else if (i == 17)
                    {
                        npc.walkAnimIndex = dataReader.ReadUInt16();
                        npc.turn180AnimIndex = dataReader.ReadUInt16();
                        npc.turn90CWAnimIndex = dataReader.ReadUInt16();
                        npc.turn90CCWAnimIndex = dataReader.ReadUInt16();
                    }
                    else if (i >= 30 && i < 40)
                    {
                        if (npc.actions == null)
                            npc.actions = new string[5];
                        npc.actions[i - 30] = dataReader.ReadString();
                        if (npc.actions[i - 30] == "hidden")
                            npc.actions[i - 30] = null;
                    }
                    else if (i == 40)
                    {
                        int colors = dataReader.ReadByte();
                        npc.recolorOriginal = new int[colors];
                        npc.recolorTarget = new int[colors];
                        for (int l = 0; l < colors; l++)
                        {
                            npc.recolorOriginal[l] = dataReader.ReadUInt16();
                            npc.recolorTarget[l] = dataReader.ReadUInt16();
                        }
                    }
                    else if (i == 60)
                    {
                        int additionalModelCount = dataReader.ReadByte();
                        npc.additionalModels = new int[additionalModelCount];
                        for (int l = 0; l < additionalModelCount; l++)
                            npc.additionalModels[l] = dataReader.ReadUInt16();
                    }
                    else if (i >= 90 && i < 93)
                        dataReader.ReadUInt16();
                    else if (i == 93)
                        npc.drawMinimapDot = false;
                    else if (i == 95)
                        npc.combatLevel = dataReader.ReadUInt16();
                    else if (i == 97)
                        npc.scaleXZ = dataReader.ReadUInt16();
                    else if (i == 98)
                        npc.scaleY = dataReader.ReadUInt16();
                    else if (i == 99)
                        npc.invisible = true;
                    else if (i == 100)
                        npc.lightModifier = dataReader.ReadByte();
                    else if (i == 101)
                        npc.shadowModifier = dataReader.ReadByte() * 5;
                    else if (i == 102)
                        npc.headIcon = dataReader.ReadUInt16();
                    else if (i == 103)
                        npc.degreesToTurn = dataReader.ReadUInt16();
                    else if (i == 106)
                    {
                        npc.varBitID = dataReader.ReadUInt16();
                        if (npc.varBitID == 65535)
                            npc.varBitID = -1;
                        npc.sessionSettingID = dataReader.ReadUInt16();
                        if (npc.sessionSettingID == 65535)
                            npc.sessionSettingID = -1;
                        int childrensCount = dataReader.ReadByte();
                        npc.childrenIDs = new int[childrensCount + 1];
                        for (int c = 0; c <= childrensCount; c++)
                        {
                            npc.childrenIDs[c] = dataReader.ReadUInt16();
                            if (npc.childrenIDs[c] == 65535)
                                npc.childrenIDs[c] = -1;
                        }
                    }
                    else if (i == 107)
                        npc.clickable = false;
                }
                while (true);
                NPCList[j] = npc;
            }
            return NPCList;
        }

        public static Color toRGB(int rgb)
        {
            int red = (rgb >> 16) & 255;
            int green = (rgb >> 8) & 255;
            int blue = (rgb) & 255;
            return Color.FromArgb(red, green, blue);
        }
    }
    public class FloorConfig
    {
        public string floorName;
        public Color mapColor = Color.Black;
        public Color actualColor = Color.Black;
        public int texture = -1;
        public bool unknown = false;
        public bool occlude = true;
        public override string ToString()
        {
            string name = floorName;
            if (name == null) name = "null";
            return "Name: " + name + ", Texture: " + texture + ", Map Color: " + mapColor + ", Actual Color: " + actualColor + ", Occlude: " + occlude;
        }
    }
    public class ItemConfig
    {
        public byte femaleYOffset = 0;
        public int value = 1;
        public int[] modifiedModelColors = null;
        public int[] originalModelColors = null;
        public bool membersObject = false;
        public int femaleEmblem = -1;
        public int certTemplateID = -1;
        public int femaleArmsID = -1;
        public int maleWornModelID = -1;
        public int maleDialogHat = -1;
        public int modelSizeX = 128;
        public string[] groundActions = null;
        public int modelInvPosOffsetX = 0;
        public string name = null;
        public int femaleDialogHat = -1;
        public int modelID = 0;
        public int maleDialog = -1;
        public bool stackable = false;
        public string description = null;
        public int certID = -1;
        public int modelInvZoom = 2000;
        public bool isMembers = true;
        public int shadowModifier = 0;
        public int maleEmblem = -1;
        public int maleArmsID = -1;
        public string[] actions = null;
        public int modelInvRotationX = 0;
        public int modelSizeZ = 128;
        public int modelSizeY = 128;
        public int[] stackIDs = null;
        public int modelInvPosOffsetY = 0;
        public int lightModifier = 0;
        public int femaleDialog = -1;
        public int modelInvRotationY = 0;
        public int femaleWornModelID = -1;
        public int[] stackAmounts = null;
        public int team = 0;
        public int diagonalRotation = 0;
        public byte maleYOffset = 0;
        public int dummy = -1;
        public int lendID, lentItemID;
    }
    public class NPCConfig
    {
        public string name;
        public string[] actions;
        public int[] childrenIDs;
        public string description;
        public int combatLevel;
        public int headIcon;
        public long type;
        public bool clickable;
        public bool invisible; //was aBoolean93
        public bool drawMinimapDot;
        public int[] npcModels;
        public int[] additionalModels;
        public int[] recolorTarget;
        public int[] recolorOriginal;
        public int scaleY;
        public int scaleXZ;
        public int shadowModifier;
        public int lightModifier;
        public byte boundDim;
        public int idleAnimation;
        public int walkAnimIndex;
        public int turn90CWAnimIndex;
        public int turn90CCWAnimIndex;
        public int turn180AnimIndex;
        public int degreesToTurn;
        public int varBitID;
        public int sessionSettingID;
    }
}
