﻿using HKLib.hk2018.hkAsyncThreadPool;
using ImGuiNET;
using SoulsFormats;
using StudioCore.Configuration;
using StudioCore.Core.Project;
using StudioCore.Editor;
using StudioCore.Editors.EmevdEditor;
using StudioCore.Editors.EmevdEditor.Actions;
using StudioCore.Editors.EmevdEditor.Tools;
using StudioCore.Editors.ParamEditor;
using StudioCore.Interface;
using StudioCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Veldrid;
using Veldrid.Sdl2;
using static StudioCore.Editors.EmevdEditor.EmevdBank;

namespace StudioCore.EmevdEditor;

public class EmevdEditorScreen : EditorScreen
{
    public bool FirstFrame { get; set; }

    public bool ShowSaveOption { get; set; }

    public ActionManager EditorActionManager = new();

    public EventScriptInfo _selectedFileInfo;
    public EMEVD _selectedScript;
    public string _selectedScriptKey;

    public EMEVD.Event _selectedEvent;
    public EMEVD.Instruction _selectedInstruction;
    public int _selectedInstructionIndex = -1;

    public EmevdEventHandler EventParameterEditor;
    public EmevdInstructionHandler InstructionParameterEditor;

    public EmevdDecorator Decorator;

    public ToolWindow ToolWindow;
    public ToolSubMenu ToolSubMenu;

    public ActionSubMenu ActionSubMenu;

    public EmevdEditorScreen(Sdl2Window window, GraphicsDevice device)
    {
        Decorator = new EmevdDecorator(this);

        EventParameterEditor = new EmevdEventHandler(this);
        InstructionParameterEditor = new EmevdInstructionHandler(this);

        ToolWindow = new ToolWindow(this);
        ToolSubMenu = new ToolSubMenu(this);
        ActionSubMenu = new ActionSubMenu(this);
    }

    public string EditorName => "EMEVD Editor##EventScriptEditor";
    public string CommandEndpoint => "emevd";
    public string SaveType => "EMEVD";

    public void Init()
    {
        ShowSaveOption = true;
    }
    public void DrawEditorMenu()
    {
        ImGui.Separator();

        if (ImGui.BeginMenu("Edit"))
        {
            UIHelper.ShowMenuIcon($"{ForkAwesome.Undo}");
            if (ImGui.MenuItem($"Undo", $"{KeyBindings.Current.CORE_UndoAction.HintText} / {KeyBindings.Current.CORE_UndoContinuousAction.HintText}", false,
                    EditorActionManager.CanUndo()))
            {
                EditorActionManager.UndoAction();
            }

            UIHelper.ShowMenuIcon($"{ForkAwesome.Undo}");
            if (ImGui.MenuItem("Undo All", "", false,
                    EditorActionManager.CanUndo()))
            {
                EditorActionManager.UndoAllAction();
            }

            UIHelper.ShowMenuIcon($"{ForkAwesome.Repeat}");
            if (ImGui.MenuItem("Redo", $"{KeyBindings.Current.CORE_RedoAction.HintText} / {KeyBindings.Current.CORE_RedoContinuousAction.HintText}", false,
                    EditorActionManager.CanRedo()))
            {
                EditorActionManager.RedoAction();
            }

            ImGui.EndMenu();
        }

        ImGui.Separator();

        ActionSubMenu.DisplayMenu();

        ImGui.Separator();

        ToolSubMenu.DisplayMenu();

        ImGui.Separator();

        if (ImGui.BeginMenu("View"))
        {
            UIHelper.ShowMenuIcon($"{ForkAwesome.Link}");
            if (ImGui.MenuItem("Files"))
            {
                UI.Current.Interface_EmevdEditor_Files = !UI.Current.Interface_EmevdEditor_Files;
            }
            UIHelper.ShowActiveStatus(UI.Current.Interface_EmevdEditor_Files);

            UIHelper.ShowMenuIcon($"{ForkAwesome.Link}");
            if (ImGui.MenuItem("Events"))
            {
                UI.Current.Interface_EmevdEditor_Events = !UI.Current.Interface_EmevdEditor_Events;
            }
            UIHelper.ShowActiveStatus(UI.Current.Interface_EmevdEditor_Events);

            UIHelper.ShowMenuIcon($"{ForkAwesome.Link}");
            if (ImGui.MenuItem("Instructions"))
            {
                UI.Current.Interface_EmevdEditor_Instructions = !UI.Current.Interface_EmevdEditor_Instructions;
            }
            UIHelper.ShowActiveStatus(UI.Current.Interface_EmevdEditor_Instructions);

            UIHelper.ShowMenuIcon($"{ForkAwesome.Link}");
            if (ImGui.MenuItem("Event Properties"))
            {
                UI.Current.Interface_EmevdEditor_EventProperties = !UI.Current.Interface_EmevdEditor_EventProperties;
            }
            UIHelper.ShowActiveStatus(UI.Current.Interface_EmevdEditor_EventProperties);

            UIHelper.ShowMenuIcon($"{ForkAwesome.Link}");
            if (ImGui.MenuItem("Instruction Properties"))
            {
                UI.Current.Interface_EmevdEditor_InstructionProperties = !UI.Current.Interface_EmevdEditor_InstructionProperties;
            }
            UIHelper.ShowActiveStatus(UI.Current.Interface_EmevdEditor_InstructionProperties);

            UIHelper.ShowMenuIcon($"{ForkAwesome.Link}");
            if (ImGui.MenuItem("Tool Window"))
            {
                UI.Current.Interface_EmevdEditor_ToolConfigurationWindow = !UI.Current.Interface_EmevdEditor_ToolConfigurationWindow;
            }
            UIHelper.ShowActiveStatus(UI.Current.Interface_EmevdEditor_ToolConfigurationWindow);

            ImGui.EndMenu();
        }
    }

    public void OnGUI(string[] initcmd)
    {
        var scale = DPI.GetUIScale();

        // Docking setup
        ImGui.PushStyleColor(ImGuiCol.Text, UI.Current.ImGui_Default_Text_Color);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4, 4) * scale);
        Vector2 wins = ImGui.GetWindowSize();
        Vector2 winp = ImGui.GetWindowPos();
        winp.Y += 20.0f * scale;
        wins.Y -= 20.0f * scale;
        ImGui.SetNextWindowPos(winp);
        ImGui.SetNextWindowSize(wins);

        var dsid = ImGui.GetID("DockSpace_EventScriptEditor");
        ImGui.DockSpace(dsid, new Vector2(0, 0), ImGuiDockNodeFlags.None);

        if (!SupportsEditor())
        {
            ImGui.Begin("Editor##InvalidEmevdEditor");

            ImGui.Text($"This editor does not support {Smithbox.ProjectType}.");

            ImGui.End();
        }
        else
        {
            if (!EmevdBank.IsLoaded)
            {
                EmevdBank.LoadEventScripts();
                EmevdBank.LoadEMEDF();
            }

            if (EmevdBank.IsLoaded && EmevdBank.IsSupported)
            {
                if (UI.Current.Interface_EmevdEditor_Files)
                {
                    EventScriptFileView();
                }
                if (UI.Current.Interface_EmevdEditor_Events)
                {
                    EventScriptEventListView();
                }
                if (UI.Current.Interface_EmevdEditor_Instructions)
                {
                    EventScriptEventInstructionView();
                }
                if (UI.Current.Interface_EmevdEditor_EventProperties)
                {
                    EventScriptEventParameterView();
                }
                if (UI.Current.Interface_EmevdEditor_InstructionProperties)
                {
                    EventScriptInstructionParameterView();
                }
            }
        }

        ToolWindow.Shortcuts();
        ToolSubMenu.Shortcuts();
        ActionSubMenu.Shortcuts();

        if (UI.Current.Interface_EmevdEditor_ToolConfigurationWindow)
        {
            ToolWindow.OnGui();
        }

        ImGui.PopStyleVar();
        ImGui.PopStyleColor(1);
    }

    private bool SupportsEditor()
    {
        return true;

        if (Smithbox.ProjectType is ProjectType.DS2 or ProjectType.DS2S)
            return true;

        return false;
    }


    private bool SelectScript = false;

    private void EventScriptFileView()
    {
        // File List
        ImGui.Begin("Files##EventScriptFileList");

        ImGui.Text($"Files");
        ImGui.Separator();

        foreach (var (info, binder) in EmevdBank.ScriptBank)
        {
            var displayName = $"{info.Name}";

            // Script row
            if (ImGui.Selectable(displayName, info.Name == _selectedScriptKey))
            {
                _selectedScriptKey = info.Name;
                _selectedFileInfo = info;
                _selectedScript = binder;
            }

            // Arrow Selection
            if (ImGui.IsItemHovered() && SelectScript)
            {
                SelectScript = false;
                _selectedScriptKey = info.Name;
                _selectedFileInfo = info;
                _selectedScript = binder;
            }
            if (ImGui.IsItemFocused() && (InputTracker.GetKey(Veldrid.Key.Up) || InputTracker.GetKey(Veldrid.Key.Down)))
            {
                SelectScript = true;
            }

            var aliasName = AliasUtils.GetMapNameAlias(info.Name);
            UIHelper.DisplayAlias(aliasName);
        }

        ImGui.End();
    }

    private bool SelectEvent = false;

    private void EventScriptEventListView()
    {
        ImGui.Begin("Events##EventListView");

        if(_selectedScript != null)
        {
            for (int i = 0; i < _selectedScript.Events.Count; i++)
            {
                var evt = _selectedScript.Events[i];

                var eventName = evt.Name;
                if (Smithbox.ProjectType is ProjectType.DS2 or ProjectType.DS2S)
                {
                    var itemName = ParamBank.PrimaryBank.GetParamFromName("ItemParam");
                    var itemRow = itemName.Rows.Where(e => e.ID == (int)evt.ID).FirstOrDefault();

                    if (itemRow != null)
                        eventName = itemRow.Name;
                }

                // Event row
                if (ImGui.Selectable($@" {evt.ID}##eventRow{i}", evt == _selectedEvent))
                {
                    _selectedEvent = evt;
                }

                // Arrow Selection
                if (ImGui.IsItemHovered() && SelectEvent)
                {
                    SelectEvent = false;
                    _selectedEvent = evt;
                }
                if (ImGui.IsItemFocused() && (InputTracker.GetKey(Veldrid.Key.Up) || InputTracker.GetKey(Veldrid.Key.Down)))
                {
                    SelectEvent = true;
                }

                UIHelper.DisplayColoredAlias(eventName, UI.Current.ImGui_AliasName_Text);
            }
        }

        ImGui.End();
    }

    private bool SelectEventInstruction = false;

    private void EventScriptEventInstructionView()
    {
        ImGui.Begin("Instructions##EventInstructionView");

        if(_selectedEvent != null)
        {
            for(int i = 0; i < _selectedEvent.Instructions.Count; i++)
            {
                var ins = _selectedEvent.Instructions[i];

                if (ImGui.Selectable($@" {ins.Bank}[{ins.ID}]##eventInstruction{i}", ins == _selectedInstruction))
                {
                    _selectedInstruction = ins;
                    _selectedInstructionIndex = i;
                }

                // Arrow Selection
                if (ImGui.IsItemHovered() && SelectEventInstruction)
                {
                    SelectEventInstruction = false;
                    _selectedInstruction = ins;
                    _selectedInstructionIndex = i;
                }
                if (ImGui.IsItemFocused() && (InputTracker.GetKey(Veldrid.Key.Up) || InputTracker.GetKey(Veldrid.Key.Down)))
                {
                    SelectEventInstruction = true;
                }

                EmevdUtils.DisplayInstructionAlias(ins);
            }

        }

        ImGui.End();
    }

    private void EventScriptEventParameterView()
    {
        ImGui.Begin("Event Properties##EventParameterView");

        EventParameterEditor.Display();

        ImGui.End();
    }

    private void EventScriptInstructionParameterView()
    {
        ImGui.Begin("Instruction Properties##InstructionParameterView");

        InstructionParameterEditor.Display();

        ImGui.End();
    }

    public void OnProjectChanged()
    {
        if (Smithbox.ProjectType != ProjectType.Undefined)
        {
            Decorator.OnProjectChanged();

            EventParameterEditor.OnProjectChanged();
            InstructionParameterEditor.OnProjectChanged();

            ToolWindow.OnProjectChanged();
            ToolSubMenu.OnProjectChanged();
            ActionSubMenu.OnProjectChanged();

            EmevdBank.LoadEventScripts();
            EmevdBank.LoadEMEDF();
        }

        ResetActionManager();
    }

    public void Save()
    {
        if (Smithbox.ProjectType == ProjectType.Undefined)
            return;

        EmevdBank.SaveEventScript(_selectedFileInfo, _selectedScript);
    }

    public void SaveAll()
    {
        if (Smithbox.ProjectType == ProjectType.Undefined)
            return;

        if (EmevdBank.IsLoaded)
            EmevdBank.SaveEventScripts();
    }

    private void ResetActionManager()
    {
        EditorActionManager.Clear();
    }
}
