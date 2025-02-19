﻿using DotNext.Collections.Generic;
using HKLib.hk2018;
using HKLib.hk2018.hkAsyncThreadPool;
using Org.BouncyCastle.Crypto;
using SoulsFormats;
using StudioCore.Banks.AliasBank;
using StudioCore.Banks.HavokAliasBank;
using StudioCore.Core.Project;
using StudioCore.Editors.HavokEditor;
using StudioCore.Editors.TimeActEditor.Bank;
using StudioCore.Interface;
using StudioCore.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static SoulsFormats.DRB;
using static StudioCore.Editors.TimeActEditor.Bank.TimeActBank;

namespace StudioCore.Editors.TimeActEditor.Utils;

public static class TimeActUtils
{
    /// <summary>
    /// Get title string for Object/Asset differentiation based on project type. 
    /// </summary>
    public static string GetObjectTitle()
    {
        string title = "Object";

        if (Smithbox.ProjectType is ProjectType.ER or ProjectType.AC6)
        {
            title = "Asset";
        }

        return title;
    }
    public static void DisplayTimeActFileAlias(string name, AliasType type)
    {
        var referenceDict = Smithbox.AliasCacheHandler.AliasCache.Characters;

        if (type == AliasType.Asset)
        {
            referenceDict = Smithbox.AliasCacheHandler.AliasCache.Assets;
        }

        var lowerName = name.ToLower();

        if (referenceDict.ContainsKey(lowerName))
        {
            var aliasName = referenceDict[lowerName].name;

            UIHelper.DisplayAlias(aliasName);
        }
    }

    public static void DisplayTimeActAlias(TimeActContainerWrapper info, int id)
    {
        if (Smithbox.BankHandler.TimeActAliases.Aliases != null)
        {
            var idStr = id.ToString();
            if (idStr.Length > 3)
            {
                var idSection = idStr.Substring(idStr.Length - 3);

                var searchStr = $"{info.Name}_{idSection}";
                var alias = Smithbox.BankHandler.TimeActAliases.Aliases.list.Where(e => e.id == searchStr)
                    .FirstOrDefault();

                if (alias != null)
                {
                    var aliasStr = alias.name;
                    UIHelper.DisplayAlias(aliasStr);
                }
                else
                {
                    UIHelper.DisplayAlias("");
                }
            }
        }
    }

    public static void DisplayAnimationAlias(TimeActSelectionHandler SelectionHandler, long id)
    {
        if (Smithbox.BankHandler.HavokGeneratorAliases != null)
        {
            List<string> aliasList = new();
            foreach (var entry in Smithbox.BankHandler.HavokGeneratorAliases.HavokAliases.List)
            {
                if (entry.ID == id.ToString())
                {
                    aliasList = entry.Generators;
                    break;
                }
            }
            if (aliasList.Count > 0)
            {
                if (CFG.Current.TimeActEditor_DisplayAllGenerators)
                {
                    UIHelper.DisplayAlias(string.Join(", ", aliasList));
                }
                else
                {
                    UIHelper.DisplayAlias(aliasList[0]);
                }
                AliasUtils.AliasTooltip(aliasList, "Generators that use this animation:");
            }
        }
    }

    public static HavokContainerInfo LoadHavokObjects(TimeActContainerWrapper info)
    {
        HavokContainerInfo newInfo = null;

        foreach (var entry in HavokFileBank.BehaviorContainerBank)
        {
            if (entry.Filename == info.Name)
            {
                entry.LoadBinder();

                foreach (var file in entry.InternalFileList)
                {
                    var name = file.Split("export")[1];
                    if (name.Contains("behaviors"))
                    {
                        newInfo = entry;
                        newInfo.LoadFile(file.ToLower());
                        newInfo.ReadHavokObjects(file.ToLower());
                        return newInfo;
                    }
                }
            }
        }

        return newInfo;
    }

    public static string GetTimeActName(int id)
    {
        var displayName = "";

        var idStr = id.ToString();
        var idSection = idStr.Substring(idStr.Length - 3);
        displayName = $"a{idSection}";

        return displayName;
    }

    public static TAE.Template GetRelevantTemplate(TemplateType type)
    {
        switch (Smithbox.ProjectType)
        {
            case ProjectType.DES:
                return TimeActTemplates["TAE.Template.DES"];
            case ProjectType.DS1:
            case ProjectType.DS1R:
                if (type is TemplateType.Character)
                {
                    return TimeActTemplates["TAE.Template.DS1"];
                }
                else if (type is TemplateType.Object)
                {
                    return TimeActTemplates["TAE.Template.DS1.OBJ"];
                }
                else if (type is TemplateType.Cutscene)
                {
                    return TimeActTemplates["TAE.Template.DS1.REMO"];
                }
                break;
            case ProjectType.DS2:
            case ProjectType.DS2S:
                return TimeActTemplates["TAE.Template.SOTFS"];
            case ProjectType.DS3:
                return TimeActTemplates["TAE.Template.DS3"];
            case ProjectType.BB:
                return TimeActTemplates["TAE.Template.BB"];
            case ProjectType.SDT:
                return TimeActTemplates["TAE.Template.SDT"];
            case ProjectType.ER:
                return TimeActTemplates["TAE.Template.ER"];
            case ProjectType.AC6:
                return TimeActTemplates["TAE.Template.AC6"];
        }

        return null;
    }

    public static void ApplyTemplate(TAE entry, TemplateType type)
    {
        switch (Smithbox.ProjectType)
        {
            case ProjectType.DES:
                entry.ApplyTemplate(TimeActTemplates["TAE.Template.DES"]);
                break;
            case ProjectType.DS1:
            case ProjectType.DS1R:
                if (type is TemplateType.Character)
                {
                    entry.ApplyTemplate(TimeActTemplates["TAE.Template.DS1"]);
                }
                else if (type is TemplateType.Object)
                {
                    entry.ApplyTemplate(TimeActTemplates["TAE.Template.DS1.OBJ"]);
                }
                else if (type is TemplateType.Cutscene)
                {
                    entry.ApplyTemplate(TimeActTemplates["TAE.Template.DS1.REMO"]);
                }
                break;
            case ProjectType.DS2:
            case ProjectType.DS2S:
                entry.ApplyTemplate(TimeActTemplates["TAE.Template.SOTFS"]);
                break;
            case ProjectType.DS3:
                entry.ApplyTemplate(TimeActTemplates["TAE.Template.DS3"]);
                break;
            case ProjectType.BB:
                entry.ApplyTemplate(TimeActTemplates["TAE.Template.BB"]);
                break;
            case ProjectType.SDT:
                entry.ApplyTemplate(TimeActTemplates["TAE.Template.SDT"]);
                break;
            case ProjectType.ER:
                entry.ApplyTemplate(TimeActTemplates["TAE.Template.ER"]);
                break;
            case ProjectType.AC6:
                entry.ApplyTemplate(TimeActTemplates["TAE.Template.AC6"]);
                break;
        }
    }

    public static TAE.Animation CloneAnimation(TAE.Animation sourceAnim)
    {
        TAE.Animation newAnim = new TAE.Animation(sourceAnim.ID, sourceAnim.MiniHeader.GetClone(), sourceAnim.AnimFileName);

        List<TAE.EventGroup> newEventGroups = new();
        foreach (var eventGrp in sourceAnim.EventGroups)
        {
            newEventGroups.Add(eventGrp.GetClone());
        }
        List<TAE.Event> newEvents = new();
        foreach (var evt in sourceAnim.Events)
        {
            newEvents.Add(evt.GetClone(false));
        }

        newAnim.EventGroups = newEventGroups;
        newAnim.Events = newEvents;

        return newAnim;
    }

    public static void SelectAdjustedAnimation(TAE.Animation targetAnim)
    {
        var handler = Smithbox.EditorHandler.TimeActEditor.SelectionHandler;
        handler.TimeActMultiselect.StoredAnimations.Clear();

        handler.CurrentTimeAct.Animations.Sort();
        for (int i = 0; i < handler.CurrentTimeAct.Animations.Count; i++)
        {
            var serAnim = handler.CurrentTimeAct.Animations[i];
            if (serAnim.ID == targetAnim.ID)
            {
                handler.CurrentTimeActAnimation = serAnim;
                handler.CurrentTimeActAnimationIndex = i;
                handler.TimeActMultiselect.StoredAnimations.Add(i, handler.CurrentTimeActAnimation);
                break;
            }
        }
    }

    public static void SelectNewAnimation(int targetIndex)
    {
        var handler = Smithbox.EditorHandler.TimeActEditor.SelectionHandler;
        handler.TimeActMultiselect.StoredAnimations.Clear();

        for (int i = 0; i < handler.CurrentTimeAct.Animations.Count; i++)
        {
            var curAnim = handler.CurrentTimeAct.Animations[i];

            if (i == targetIndex)
            {
                handler.CurrentTimeActAnimation = curAnim;
                handler.CurrentTimeActAnimationIndex = i;
                handler.TimeActMultiselect.StoredAnimations.Add(i, handler.CurrentTimeActAnimation);
                break;
            }
        }
    }

    public static void SelectNewEvent(int targetIndex)
    {
        var handler = Smithbox.EditorHandler.TimeActEditor.SelectionHandler;
        handler.TimeActMultiselect.StoredEvents.Clear();

        for (int i = 0; i < handler.CurrentTimeActAnimation.Events.Count; i++)
        {
            var curEvent = handler.CurrentTimeActAnimation.Events[i];

            if (i == targetIndex)
            {
                handler.CurrentTimeActEvent = curEvent;
                handler.CurrentTimeActEventIndex = i;
                handler.TimeActMultiselect.StoredEvents.Add(i, handler.CurrentTimeActEvent);
                break;
            }
        }
    }
    public enum TemplateType
    {
        Character,
        Object,
        Cutscene
    }

    public enum AliasType
    {
        Character,
        Asset
    }
}
