﻿using ImGuiNET;
using StudioCore.Editor;
using StudioCore.Resource;
using StudioCore.Scene;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.Utilities;
using Viewport = StudioCore.Interface.Viewport;
using StudioCore.Configuration;
using StudioCore.MsbEditor;
using StudioCore.Editors.MapEditor;
using StudioCore.Utilities;
using StudioCore.Editors.ModelEditor.Actions;
using StudioCore.Core.Project;
using StudioCore.Interface;

namespace StudioCore.Editors.ModelEditor;

// DESIGN:
// Actual Model is loaded as CurrentFLVER. This is the object that all edits apply to.
// Viewport Model is loaded via the Resource Manager system and is represented via the Model Container instance.
// Viewport Model is never edited, and instead it is discarded and re-generated from saved actual model when required.

// PITFALLS:
// Actual Model and Viewport Model must be manually kept in sync when entries are added/removed.
// Default method is to add to actual model, force actual model save and then re-load.
public class ModelEditorScreen : EditorScreen
{
    public bool FirstFrame { get; set; }

    public bool ShowSaveOption { get; set; }

    public MapEditor.ViewportActionManager EditorActionManager = new();

    public ModelSelectionView ModelSelectionView;

    public ModelPropertyEditor ModelPropertyEditor;
    public ModelPropertyCache _propCache = new();

    public ModelHierarchyView ModelHierarchy;
    public ViewportSelection _selection = new();

    public Universe _universe;

    public Rectangle Rect;
    public RenderScene RenderScene;
    public IViewport Viewport;

    public bool ViewportUsingKeyboard;
    public Sdl2Window Window;

    public ModelResourceHandler ResourceHandler;
    public ModelViewportHandler ViewportHandler;
    public SkeletonHandler SkeletonHandler;

    public ToolWindow ToolWindow;
    public ToolSubMenu ToolSubMenu;

    public ActionSubMenu ActionSubMenu;

    public ModelEditorScreen(Sdl2Window window, GraphicsDevice device)
    {
        Rect = window.Bounds;
        Window = window;

        if (device != null)
        {
            RenderScene = new RenderScene();
            Viewport = new Viewport(ViewportType.ModelEditor, "Modeleditvp", device, RenderScene, EditorActionManager, _selection, Rect.Width, Rect.Height);
        }
        else
        {
            Viewport = new NullViewport(ViewportType.ModelEditor, "Modeleditvp", EditorActionManager, _selection, Rect.Width, Rect.Height);
        }

        _universe = new Universe(RenderScene, _selection);

        ResourceHandler = new ModelResourceHandler(this, Viewport);
        ViewportHandler = new ModelViewportHandler(this, Viewport);
        ModelSelectionView = new ModelSelectionView(this);
        ModelHierarchy = new ModelHierarchyView(this);
        ModelPropertyEditor = new ModelPropertyEditor(this);
        SkeletonHandler = new SkeletonHandler(this, _universe);

        ToolWindow = new ToolWindow(this);
        ToolSubMenu = new ToolSubMenu(this);
        ActionSubMenu = new ActionSubMenu(this);
    }

    public void Init()
    {
        ShowSaveOption = true;
    }

    public string EditorName => "Model Editor";
    public string CommandEndpoint => "model";
    public string SaveType => "Models";

    public void Update(float dt)
    {
        ViewportUsingKeyboard = Viewport.Update(Window, dt);

        /*
        if (ViewportHandler._flverhandle != null)
        {
            FlverResource r = ViewportHandler._flverhandle.Get();
            _universe.LoadFlverInModelEditor(r.Flver, ViewportHandler._renderMesh, ResourceHandler.CurrentFLVERInfo.ModelName);

            if (CFG.Current.Viewport_Enable_Texturing)
            {
                _universe.ScheduleTextureRefresh();
            }
        }
        */

        if (ResourceHandler._loadingTask != null && ResourceHandler._loadingTask.IsCompleted)
        {
            ResourceHandler._loadingTask = null;
        }
    }

    public void EditorResized(Sdl2Window window, GraphicsDevice device)
    {
        Window = window;
        Rect = window.Bounds;
        //Viewport.ResizeViewport(device, new Rectangle(0, 0, window.Width, window.Height));
    }

    public void Draw(GraphicsDevice device, CommandList cl)
    {
        if (Viewport != null)
        {
            Viewport.Draw(device, cl);
        }
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
            if (ImGui.MenuItem("Viewport"))
            {
                UI.Current.Interface_Editor_Viewport = !UI.Current.Interface_Editor_Viewport;
            }
            UIHelper.ShowActiveStatus(UI.Current.Interface_Editor_Viewport);

            UIHelper.ShowMenuIcon($"{ForkAwesome.Link}");
            if (ImGui.MenuItem("Model Hierarchy"))
            {
                UI.Current.Interface_ModelEditor_ModelHierarchy = !UI.Current.Interface_ModelEditor_ModelHierarchy;
            }
            UIHelper.ShowActiveStatus(UI.Current.Interface_ModelEditor_ModelHierarchy);

            UIHelper.ShowMenuIcon($"{ForkAwesome.Link}");
            if (ImGui.MenuItem("Properties"))
            {
                UI.Current.Interface_ModelEditor_Properties = !UI.Current.Interface_ModelEditor_Properties;
            }
            UIHelper.ShowActiveStatus(UI.Current.Interface_ModelEditor_Properties);

            UIHelper.ShowMenuIcon($"{ForkAwesome.Link}");
            if (ImGui.MenuItem("Asset Browser"))
            {
                UI.Current.Interface_ModelEditor_AssetBrowser = !UI.Current.Interface_ModelEditor_AssetBrowser;
            }
            UIHelper.ShowActiveStatus(UI.Current.Interface_ModelEditor_AssetBrowser);

            UIHelper.ShowMenuIcon($"{ForkAwesome.Link}");
            if (ImGui.MenuItem("Tool Window"))
            {
                UI.Current.Interface_ModelEditor_ToolConfigurationWindow = !UI.Current.Interface_ModelEditor_ToolConfigurationWindow;
            }
            UIHelper.ShowActiveStatus(UI.Current.Interface_ModelEditor_ToolConfigurationWindow);

            UIHelper.ShowMenuIcon($"{ForkAwesome.Link}");
            if (ImGui.MenuItem("Profiling"))
            {
                UI.Current.Interface_Editor_Profiling = !UI.Current.Interface_Editor_Profiling;
            }
            UIHelper.ShowActiveStatus(UI.Current.Interface_Editor_Profiling);

            UIHelper.ShowMenuIcon($"{ForkAwesome.Link}");
            if (ImGui.MenuItem("Resource List"))
            {
                UI.Current.Interface_ModelEditor_ResourceList = !UI.Current.Interface_ModelEditor_ResourceList;
            }
            UIHelper.ShowActiveStatus(UI.Current.Interface_ModelEditor_ResourceList);

            UIHelper.ShowMenuIcon($"{ForkAwesome.Link}");
            if (ImGui.MenuItem("Viewport Grid"))
            {
                UI.Current.Interface_ModelEditor_Viewport_Grid = !UI.Current.Interface_ModelEditor_Viewport_Grid;
                CFG.Current.ModelEditor_Viewport_RegenerateMapGrid = true;
            }
            UIHelper.ShowActiveStatus(UI.Current.Interface_ModelEditor_Viewport_Grid);

            ImGui.EndMenu();
        }

        ImGui.Separator();

        if (ImGui.BeginMenu("Filters", RenderScene != null && Viewport != null))
        {
            var container = _universe.LoadedModelContainers[ViewportHandler.ContainerID];

            UIHelper.ShowMenuIcon($"{ForkAwesome.Eye}");
            if (ImGui.MenuItem("Meshes"))
            {
                CFG.Current.ModelEditor_ViewMeshes = !CFG.Current.ModelEditor_ViewMeshes;
                foreach (var entry in container.Mesh_RootNode.Children)
                {
                    entry.EditorVisible = CFG.Current.ModelEditor_ViewMeshes;
                }
            }
            UIHelper.ShowActiveStatus(CFG.Current.ModelEditor_ViewMeshes);

            UIHelper.ShowMenuIcon($"{ForkAwesome.Eye}");
            if (ImGui.MenuItem("Dummy Polygons"))
            {
                CFG.Current.ModelEditor_ViewDummyPolys = !CFG.Current.ModelEditor_ViewDummyPolys;

                foreach(var entry in container.DummyPoly_RootNode.Children)
                {
                    entry.EditorVisible = CFG.Current.ModelEditor_ViewDummyPolys;
                }
            }
            UIHelper.ShowActiveStatus(CFG.Current.ModelEditor_ViewDummyPolys);

            UIHelper.ShowMenuIcon($"{ForkAwesome.Eye}");
            if (ImGui.MenuItem("Bones"))
            {
                CFG.Current.ModelEditor_ViewBones = !CFG.Current.ModelEditor_ViewBones;
                foreach (var entry in container.Bone_RootNode.Children)
                {
                    entry.EditorVisible = CFG.Current.ModelEditor_ViewBones;
                }
            }
            UIHelper.ShowActiveStatus(CFG.Current.ModelEditor_ViewBones);

            if (Smithbox.ProjectType is ProjectType.ER)
            {
                UIHelper.ShowMenuIcon($"{ForkAwesome.Eye}");
                if (ImGui.MenuItem("Collision (High)"))
                {
                    CFG.Current.ModelEditor_ViewHighCollision = !CFG.Current.ModelEditor_ViewHighCollision;

                    foreach (var entry in container.Collision_RootNode.Children)
                    {
                        var colEntity = (CollisionEntity)entry;
                        if (colEntity.HavokCollisionType is HavokCollisionType.High)
                        {
                            colEntity.EditorVisible = CFG.Current.ModelEditor_ViewHighCollision;
                        }
                    }
                }
                UIHelper.ShowActiveStatus(CFG.Current.ModelEditor_ViewHighCollision);

                UIHelper.ShowMenuIcon($"{ForkAwesome.Eye}");
                if (ImGui.MenuItem("Collision (Low)"))
                {
                    CFG.Current.ModelEditor_ViewLowCollision = !CFG.Current.ModelEditor_ViewLowCollision;

                    foreach (var entry in container.Collision_RootNode.Children)
                    {
                        var colEntity = (CollisionEntity)entry;
                        if (colEntity.HavokCollisionType is HavokCollisionType.Low)
                        {
                            colEntity.EditorVisible = CFG.Current.ModelEditor_ViewLowCollision;
                        }
                    }
                }
                UIHelper.ShowActiveStatus(CFG.Current.ModelEditor_ViewLowCollision);
            }

            /*
            ImguiUtils.ShowMenuIcon($"{ForkAwesome.Eye}");
            if (ImGui.MenuItem("Skeleton"))
            {
                CFG.Current.ModelEditor_ViewSkeleton = !CFG.Current.ModelEditor_ViewSkeleton;
            }
            ImguiUtils.ShowActiveStatus(CFG.Current.ModelEditor_ViewSkeleton);
            */

            ImGui.EndMenu();
        }

        ImGui.Separator();

        if (ImGui.BeginMenu("Viewport"))
        {
            UIHelper.ShowMenuIcon($"{ForkAwesome.LightbulbO}");
            if (ImGui.BeginMenu("Scene Lighting"))
            {
                Viewport.SceneParamsGui();
                ImGui.EndMenu();
            }

            ImGui.EndMenu();
        }

        ImGui.Separator();

        if (ImGui.BeginMenu("Gizmos"))
        {
            UIHelper.ShowMenuIcon($"{ForkAwesome.Compass}");
            if (ImGui.BeginMenu("Mode"))
            {
                if (ImGui.MenuItem("Translate", KeyBindings.Current.VIEWPORT_GizmoTranslationMode.HintText,
                        Gizmos.Mode == Gizmos.GizmosMode.Translate))
                {
                    Gizmos.Mode = Gizmos.GizmosMode.Translate;
                }

                if (ImGui.MenuItem("Rotate", KeyBindings.Current.VIEWPORT_GizmoRotationMode.HintText,
                        Gizmos.Mode == Gizmos.GizmosMode.Rotate))
                {
                    Gizmos.Mode = Gizmos.GizmosMode.Rotate;
                }

                ImGui.EndMenu();
            }

            UIHelper.ShowMenuIcon($"{ForkAwesome.Cube}");
            if (ImGui.BeginMenu("Space"))
            {
                if (ImGui.MenuItem("Local", KeyBindings.Current.VIEWPORT_GizmoSpaceMode.HintText,
                        Gizmos.Space == Gizmos.GizmosSpace.Local))
                {
                    Gizmos.Space = Gizmos.GizmosSpace.Local;
                }

                if (ImGui.MenuItem("World", KeyBindings.Current.VIEWPORT_GizmoSpaceMode.HintText,
                        Gizmos.Space == Gizmos.GizmosSpace.World))
                {
                    Gizmos.Space = Gizmos.GizmosSpace.World;
                }

                ImGui.EndMenu();
            }

            UIHelper.ShowMenuIcon($"{ForkAwesome.Cubes}");
            if (ImGui.BeginMenu("Origin"))
            {
                if (ImGui.MenuItem("World", KeyBindings.Current.VIEWPORT_GizmoOriginMode.HintText,
                        Gizmos.Origin == Gizmos.GizmosOrigin.World))
                {
                    Gizmos.Origin = Gizmos.GizmosOrigin.World;
                }

                if (ImGui.MenuItem("Bounding Box", KeyBindings.Current.VIEWPORT_GizmoOriginMode.HintText,
                        Gizmos.Origin == Gizmos.GizmosOrigin.BoundingBox))
                {
                    Gizmos.Origin = Gizmos.GizmosOrigin.BoundingBox;
                }

                ImGui.EndMenu();
            }

            ImGui.EndMenu();
        }
    }

    public void OnGUI(string[] initcmd)
    {
        var scale = DPI.GetUIScale();
        // Docking setup
        //var vp = ImGui.GetMainViewport();
        Vector2 wins = ImGui.GetWindowSize();
        Vector2 winp = ImGui.GetWindowPos();
        winp.Y += 20.0f * scale;
        wins.Y -= 20.0f * scale;
        ImGui.SetNextWindowPos(winp);
        ImGui.SetNextWindowSize(wins);
        var dsid = ImGui.GetID("DockSpace_ModelEdit");
        ImGui.DockSpace(dsid, new Vector2(0, 0));

        // Keyboard shortcuts
        if (EditorActionManager.CanUndo() && InputTracker.GetKeyDown(KeyBindings.Current.CORE_UndoAction))
        {
            EditorActionManager.UndoAction();
        }

        if (EditorActionManager.CanUndo() && InputTracker.GetKey(KeyBindings.Current.CORE_UndoContinuousAction))
        {
            EditorActionManager.UndoAction();
        }

        if (EditorActionManager.CanRedo() && InputTracker.GetKeyDown(KeyBindings.Current.CORE_RedoAction))
        {
            EditorActionManager.RedoAction();
        }

        if (EditorActionManager.CanRedo() && InputTracker.GetKey(KeyBindings.Current.CORE_RedoContinuousAction))
        {
            EditorActionManager.RedoAction();
        }

        ActionSubMenu.Shortcuts();
        ToolSubMenu.Shortcuts();

        if (!ViewportUsingKeyboard && !ImGui.GetIO().WantCaptureKeyboard)
        {
            if (InputTracker.GetKeyDown(KeyBindings.Current.VIEWPORT_GizmoTranslationMode))
            {
                Gizmos.Mode = Gizmos.GizmosMode.Translate;
            }

            if (InputTracker.GetKeyDown(KeyBindings.Current.VIEWPORT_GizmoRotationMode))
            {
                Gizmos.Mode = Gizmos.GizmosMode.Rotate;
            }

            // Use home key to cycle between gizmos origin modes
            if (InputTracker.GetKeyDown(KeyBindings.Current.VIEWPORT_GizmoOriginMode))
            {
                if (Gizmos.Origin == Gizmos.GizmosOrigin.World)
                {
                    Gizmos.Origin = Gizmos.GizmosOrigin.BoundingBox;
                }
                else if (Gizmos.Origin == Gizmos.GizmosOrigin.BoundingBox)
                {
                    Gizmos.Origin = Gizmos.GizmosOrigin.World;
                }
            }

            // F key frames the selection
            if (InputTracker.GetKeyDown(KeyBindings.Current.MAP_FrameSelection))
            {
                HashSet<Entity> selected = _selection.GetFilteredSelection<Entity>();
                var first = false;
                BoundingBox box = new();
                foreach (Entity s in selected)
                {
                    if (s.RenderSceneMesh != null)
                    {
                        if (!first)
                        {
                            box = s.RenderSceneMesh.GetBounds();
                            first = true;
                        }
                        else
                        {
                            box = BoundingBox.Combine(box, s.RenderSceneMesh.GetBounds());
                        }
                    }
                }

                if (first)
                {
                    Viewport.FrameBox(box);
                }
            }

            // Render settings
            if (InputTracker.GetControlShortcut(Key.Number1))
            {
                RenderScene.DrawFilter = RenderFilter.MapPiece | RenderFilter.Object | RenderFilter.Character |
                                         RenderFilter.Region;
            }
            else if (InputTracker.GetControlShortcut(Key.Number2))
            {
                RenderScene.DrawFilter = RenderFilter.Collision | RenderFilter.Object | RenderFilter.Character |
                                         RenderFilter.Region;
            }
            else if (InputTracker.GetControlShortcut(Key.Number3))
            {
                RenderScene.DrawFilter = RenderFilter.Collision | RenderFilter.Navmesh | RenderFilter.Object |
                                         RenderFilter.Character | RenderFilter.Region;
            }
        }

        if (initcmd != null && initcmd.Length > 1)
        {
            if (initcmd[0] == "load")
            {
                var modelName = initcmd[1];
                var assetType = initcmd[2];

                if (assetType == "Character")
                {
                    ModelSelectionView._searchInput = modelName;
                    ResourceHandler.LoadCharacter(modelName);
                }

                if (assetType == "Asset")
                {
                    ModelSelectionView._searchInput = modelName;
                    ResourceHandler.LoadAsset(modelName);
                }

                if (assetType == "Part")
                {
                    ModelSelectionView._searchInput = modelName;
                    ResourceHandler.LoadPart(modelName);
                }

                if(initcmd.Length > 3)
                {
                    var mapId = initcmd[3];

                    if (assetType == "MapPiece")
                    {
                        var mapPieceName = modelName.Replace(mapId, "m");
                        ModelSelectionView._searchInput = mapPieceName;
                        ResourceHandler.LoadMapPiece(modelName, mapId);
                    }
                }
            }
        }

        ImGui.PushStyleColor(ImGuiCol.Text, UI.Current.ImGui_Default_Text_Color);
        ImGui.SetNextWindowSize(new Vector2(300, 500) * scale, ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new Vector2(20, 20) * scale, ImGuiCond.FirstUseEver);

        Vector3 clear_color = new(114f / 255f, 144f / 255f, 154f / 255f);
        //ImGui.Text($@"Viewport size: {Viewport.Width}x{Viewport.Height}");
        //ImGui.Text(string.Format("Application average {0:F3} ms/frame ({1:F1} FPS)", 1000f / ImGui.GetIO().Framerate, ImGui.GetIO().Framerate));

        Viewport.OnGui();
        ModelSelectionView.OnGui();
        ModelHierarchy.OnGui();
        ModelPropertyEditor.OnGui();
        SkeletonHandler.OnGui();

        if (UI.Current.Interface_ModelEditor_ToolConfigurationWindow)
        {
            ToolWindow.OnGui();
        }

        ResourceLoadWindow.DisplayWindow(Viewport.Width, Viewport.Height);

        if (UI.Current.Interface_ModelEditor_ResourceList)
        {
            ResourceListWindow.DisplayWindow("modelResourceList");
        }
        ImGui.PopStyleColor(1);

        // Focus on Properties by default when this editor is made focused
        if (FirstFrame)
        {
            ImGui.SetWindowFocus("Properties##ModelEditorProperties");

            FirstFrame = false;
        }
    }

    public bool InputCaptured()
    {
        return Viewport.ViewportSelected;
    }

    public void OnProjectChanged()
    {
        if (Smithbox.ProjectType != ProjectType.Undefined)
        {
            ModelSelectionView.OnProjectChanged();
            ModelHierarchy.OnProjectChanged();
            ToolWindow.OnProjectChanged();
            ToolSubMenu.OnProjectChanged();
            ActionSubMenu.OnProjectChanged();
            ViewportHandler.OnProjectChanged();
        }

        ResourceHandler.OnProjectChange();
        _universe.UnloadAll(true);
    }

    public void Save()
    {
        if (Smithbox.ProjectType == ProjectType.Undefined)
            return;

        ResourceHandler.SaveModel();
    }

    public void SaveAll()
    {
        if (Smithbox.ProjectType == ProjectType.Undefined)
            return;

        Save(); // Just call save.
    }

}
