using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using VaMUtils;

namespace ThatsLewd
{
  public partial class CharacterStateManager : MVRScript
  {
    public static class Tabs
    {
      public const string Info = "Info";
      public const string Groups = "Groups";
      public const string States = "States";
      public const string Layers = "Layers";
      public const string Animations = "Animations";
      public const string Keyframes = "Keyframes";
      public const string Transitions = "Transitions";
      public const string Triggers = "Triggers";

      public static readonly string[] list = new string[] { Info, Groups, States, Layers, Animations, Keyframes, Transitions, Triggers };
    }

    bool uiNeedsRebuilt = false;
    List<object> uiItems = new List<object>();

    GameObject tabBarPrefab;
    string activeTab;

    JSONStorableBool hideTopUIStorable;
    JSONStorableBool playbackEnabledStorable;

    JSONStorableStringChooser activeGroupIdStorable;
    JSONStorableStringChooser activeStateIdStorable;
    JSONStorableStringChooser activeLayerIdStorable;
    JSONStorableStringChooser activeAnimationIdStorable;

    Group activeGroup = null;
    State activeState = null;
    Layer activeLayer = null;
    Animation activeAnimation = null;

    JSONStorableString editGroupNameStorable;
    JSONStorableString editStateNameStorable;
    JSONStorableString editLayerNameStorable;
    JSONStorableString editAnimationNameStorable;

    void UIInit()
    {
      UIBuilder.Init(this, CreateUIElement);
      RebuildUI();
    }

    void UIDestroy()
    {
      UIBuilder.Destroy();
      Utils.SafeDestroy(ref tabBarPrefab);
    }

    void UIUpdate()
    {
      if (uiNeedsRebuilt)
      {
        uiNeedsRebuilt = false;
        RebuildUI();
      }
    }

    void InvalidateUI()
    {
      uiNeedsRebuilt = true;
    }

    void UI(object item)
    {
      uiItems.Add(item);
    }

    void RebuildUI()
    {
      UIBuilder.RemoveUIElements(ref uiItems);

      UI(UIBuilder.CreateToggle(ref playbackEnabledStorable, UIColumn.LEFT, "Playback Enabled", true));
      UI(UIBuilder.CreateToggle(ref hideTopUIStorable, UIColumn.RIGHT, "Hide Top UI", false, callback: HandleHideTopUI));

      if (!hideTopUIStorable.val)
      {
        UI(UIBuilder.CreateStringChooserKeyVal(ref activeGroupIdStorable, UIColumn.LEFT, "Group", callback: HandleSelectGroup));
        UI(UIBuilder.CreateStringChooserKeyVal(ref activeStateIdStorable, UIColumn.RIGHT, "State", callback: HandleSelectState));
        UI(UIBuilder.CreateStringChooserKeyVal(ref activeLayerIdStorable, UIColumn.LEFT, "Layer", callback: HandleSelectLayer));
        UI(UIBuilder.CreateStringChooserKeyVal(ref activeAnimationIdStorable, UIColumn.RIGHT, "Animation", callback: HandleSelectAnimation));

        UI(UIBuilder.CreateTabBar(ref tabBarPrefab, UIColumn.LEFT, Tabs.list, HandleTabSelect, 5));
        UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 2 * 55f));
      }

      BuildActiveTabUI();
    }

    void HandleHideTopUI(bool val)
    {
      InvalidateUI();
    }

    void HandleTabSelect(string tabName)
    {
      activeTab = tabName;
      InvalidateUI();
    }

    void BuildActiveTabUI()
    {
      switch (activeTab)
      {
        case Tabs.Info:
          BuildInfoTabUI();
          break;
        case Tabs.Groups:
          BuildGroupsTabUI();
          break;
        case Tabs.States:
          BuildStatesTabUI();
          break;
        case Tabs.Layers:
          BuildLayersTabUI();
          break;
        case Tabs.Animations:
          BuildAnimationsTabUI();
          break;
        case Tabs.Keyframes:
          BuildKeyframesTabUI();
          break;
        case Tabs.Transitions:
          BuildTransitionsTabUI();
          break;
        case Tabs.Triggers:
          BuildTriggersTabUI();
          break;
        default:
          BuildInfoTabUI();
          break;
      }
    }

    void HandleSelectGroup(string val)
    {
      Utils.EnsureStringChooserValue(activeGroupIdStorable, defaultToFirstChoice: true, noCallback: true);
      activeGroup = Group.list.Find((g) => g.id == activeGroupIdStorable.val);

      RefreshStateList();
      Utils.SelectStringChooserFirstValue(activeStateIdStorable);

      if (editGroupNameStorable != null)
      {
        editGroupNameStorable.valNoCallback = activeGroup?.name ?? "";
      }

      InvalidateUI();
    }

    void HandleSelectState(string val)
    {
      Utils.EnsureStringChooserValue(activeStateIdStorable, defaultToFirstChoice: true, noCallback: true);
      activeState = activeGroup?.states.Find((s) => s.id == activeStateIdStorable.val);

      if (editStateNameStorable != null)
      {
        editStateNameStorable.valNoCallback = activeState?.name ?? "";
      }

      InvalidateUI();
    }

    void HandleSelectLayer(string val)
    {
      Utils.EnsureStringChooserValue(activeLayerIdStorable, defaultToFirstChoice: true, noCallback: true);
      activeLayer = Layer.list.Find((l) => l.id == activeLayerIdStorable.val);

      RefreshAnimationList();
      Utils.SelectStringChooserFirstValue(activeAnimationIdStorable);

      if (editLayerNameStorable != null)
      {
        editLayerNameStorable.valNoCallback = activeLayer?.name ?? "";
      }

      InvalidateUI();
    }

    void HandleSelectAnimation(string val)
    {
      Utils.EnsureStringChooserValue(activeAnimationIdStorable, defaultToFirstChoice: true, noCallback: true);
      activeAnimation = activeLayer?.animations.Find((a) => a.id == activeAnimationIdStorable.val);

      if (editAnimationNameStorable != null)
      {
        editAnimationNameStorable.valNoCallback = activeAnimation?.name ?? "";
      }

      InvalidateUI();
    }

    void RefreshGroupList()
    {
      List<KeyValuePair<string, string>> entries = new List<KeyValuePair<string, string>>();
      foreach (Group group in Group.list)
      {
        entries.Add(new KeyValuePair<string, string>(group.id, group.name));
      }
      entries.Sort((a, b) => a.Value.CompareTo(b.Value));
      Utils.SetStringChooserChoices(activeGroupIdStorable, entries);

      InvalidateUI();
    }

    void RefreshStateList()
    {
      List<KeyValuePair<string, string>> entries = new List<KeyValuePair<string, string>>();
      if (activeGroup != null)
      {
        foreach (State state in activeGroup.states)
        {
          entries.Add(new KeyValuePair<string, string>(state.id, state.name));
        }
      }
      entries.Sort((a, b) => a.Value.CompareTo(b.Value));
      Utils.SetStringChooserChoices(activeStateIdStorable, entries);

      InvalidateUI();
    }

    void RefreshLayerList()
    {
      List<KeyValuePair<string, string>> entries = new List<KeyValuePair<string, string>>();
      foreach (Layer layer in Layer.list)
      {
        entries.Add(new KeyValuePair<string, string>(layer.id, layer.name));
      }
      entries.Sort((a, b) => a.Value.CompareTo(b.Value));
      Utils.SetStringChooserChoices(activeLayerIdStorable, entries);

      InvalidateUI();
    }

    void RefreshAnimationList()
    {
      List<KeyValuePair<string, string>> entries = new List<KeyValuePair<string, string>>();
      if (activeLayer != null)
      {
        foreach (Animation animation in activeLayer.animations)
        {
          entries.Add(new KeyValuePair<string, string>(animation.id, animation.name));
        }
      }
      entries.Sort((a, b) => a.Value.CompareTo(b.Value));
      Utils.SetStringChooserChoices(activeAnimationIdStorable, entries);

      InvalidateUI();
    }

    void CreateMainHeader(UIColumn side, string text)
    {
      UI(UIBuilder.CreateHeaderText(side, text, 50f));
    }

    void CreateSubHeader(UIColumn side, string text)
    {
      UI(UIBuilder.CreateHeaderText(side, text, 38f));
    }

    void CreateBasicFunctionsUI(string category, bool showNewOnly, Action createNameInput, UnityAction newHandler, UnityAction duplicateHandler, UnityAction deleteHandler)
    {
      if (showNewOnly)
      {
        UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 50f));
        UI(UIBuilder.CreateButton(UIColumn.RIGHT, $"New {category}", newHandler));
      }
      else
      {
        createNameInput();
        UI(UIBuilder.CreateButtonPair(UIColumn.RIGHT, $"New {category}", newHandler, $"Duplicate {category}", duplicateHandler));
        UI(UIBuilder.CreateSpacer(UIColumn.RIGHT));
        UI(UIBuilder.CreateButton(UIColumn.RIGHT, $"Delete {category}", deleteHandler, color: UIColor.RED));
        UI(UIBuilder.CreateSpacer(UIColumn.RIGHT));
      }
    }

    void CreateBottomPadding()
    {
      UI(UIBuilder.CreateSpacer(UIColumn.LEFT, 100f));
      UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 100f));
    }


    // ========================================================================== //
    // ================================ INFO TAB ================================ //
    // ========================================================================== //
    void BuildInfoTabUI()
    {
      CreateMainHeader(UIColumn.LEFT, "Info");
      UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 45f));

      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"Info tab",
        1
      ));


      CreateBottomPadding();
    }


    // ============================================================================ //
    // ================================ GROUPS TAB ================================ //
    // ============================================================================ //
    void BuildGroupsTabUI()
    {
      CreateMainHeader(UIColumn.LEFT, "Groups");
      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"A <b>Group</b> is a collection of <b>States</b> that are independent of other groups. Only one state can be active per group. For example, you might use one group for walking and one group for gestures.",
        185f
      ));

      CreateBasicFunctionsUI(
        "Group",
        activeGroup == null,
        () => { UI(UIBuilder.CreateOnelineTextInput(ref editGroupNameStorable, UIColumn.RIGHT, "Name", activeGroup?.name ?? "", callback: HandleRenameGroup)); },
        HandleNewGroup,
        HandleDuplicateGroup,
        HandleDeleteGroup
      );

      if (activeGroup == null) return;


      CreateBottomPadding();
    }

    void HandleNewGroup()
    {
      Group group = new Group();
      RefreshGroupList();
      activeGroupIdStorable.val = group.id;
    }

    void HandleDuplicateGroup()
    {
      if (activeGroup == null) return;
      Group group = activeGroup.Clone();
      RefreshGroupList();
      activeGroupIdStorable.val = group.id;
    }

    void HandleRenameGroup(string val)
    {
      if (activeGroup == null) return;
      activeGroup.name = val;
      RefreshGroupList();
      activeGroupIdStorable.val = activeGroup.id;
    }

    void HandleDeleteGroup()
    {
      if (activeGroup == null) return;
      activeGroup.Delete();
      RefreshGroupList();
      Utils.SelectStringChooserFirstValue(activeGroupIdStorable);
    }


    // ============================================================================ //
    // ================================ STATES TAB ================================ //
    // ============================================================================ //
    void BuildStatesTabUI()
    {
      CreateMainHeader(UIColumn.LEFT, "States");
      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"A <b>State</b> defines what a character is currently doing (idle, sitting, etc). A state assigns <b>Animations</b> to layers that can be played either sequentially or randomly.",
        185f
      ));

      if (activeGroup == null)
      {
        UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 50f));
        UI(UIBuilder.CreateInfoTextNoScroll(
          UIColumn.RIGHT,
          @"You must select a <b>Group</b> before you can create any <b>States</b>.",
          2
        ));
        return;
      }

      CreateBasicFunctionsUI(
        "State",
        activeState == null,
        () => { UI(UIBuilder.CreateOnelineTextInput(ref editStateNameStorable, UIColumn.RIGHT, "Name", activeState?.name ?? "", callback: HandleRenameState)); },
        HandleNewState,
        HandleDuplicateState,
        HandleDeleteState
      );

      if (activeState == null) return;


      CreateBottomPadding();
    }

    void HandleNewState()
    {
      if (activeGroup == null) return;
      State state = new State(activeGroup);
      RefreshStateList();
      activeStateIdStorable.val = state.id;
    }

    void HandleDuplicateState()
    {
      if (activeState == null) return;
      State state = activeState.Clone();
      RefreshStateList();
      activeStateIdStorable.val = state.id;
    }

    void HandleRenameState(string val)
    {
      if (activeState == null) return;
      activeState.name = val;
      RefreshStateList();
      activeStateIdStorable.val = activeState.id;
    }

    void HandleDeleteState()
    {
      if (activeState == null) return;
      activeState.Delete();
      RefreshStateList();
      Utils.SelectStringChooserFirstValue(activeStateIdStorable);
    }


    // ============================================================================ //
    // ================================ LAYERS TAB ================================ //
    // ============================================================================ //
    JSONStorableStringChooser morphChooserStorable;
    JSONStorableBool morphChooserUseFavoritesStorable;

    void BuildLayersTabUI()
    {
      CreateMainHeader(UIColumn.LEFT, "Layers");
      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"A <b>Layer</b> is a set of controllers and/or morphs that are acted upon by an <b>Animation</b>. Layers allow a character to have several independently animated parts.",
        185f
      ));

      CreateBasicFunctionsUI(
        "Layer",
        activeLayer == null,
        () => { UI(UIBuilder.CreateOnelineTextInput(ref editLayerNameStorable, UIColumn.RIGHT, "Name", activeLayer?.name ?? "", callback: HandleRenameLayer)); },
        HandleNewLayer,
        HandleDuplicateLayer,
        HandleDeleteLayer
      );

      if (activeLayer == null) return;

      CreateSubHeader(UIColumn.LEFT, "Controllers");
      UI(UIBuilder.CreateButtonPair(UIColumn.LEFT, "Select All Position", () => { HandleSelectAllControllers(true, false); }, "Select All Rotation", () => { HandleSelectAllControllers(false, true); }));
      UI(UIBuilder.CreateButton(UIColumn.LEFT, "Deselect All", HandleDeselectAllControllers));
      foreach (TrackedController tc in activeLayer.trackedControllers)
      {
        UI(CreateControllerSelector(tc, UIColumn.LEFT));
      }

      CreateSubHeader(UIColumn.RIGHT, "Morphs");
      UI(UIBuilder.CreateToggle(ref morphChooserUseFavoritesStorable, UIColumn.RIGHT, "Favorites Only", true, callback: HandleToggleMorphChooserFavorites));
      UI(UIBuilder.CreateButton(UIColumn.RIGHT, "Force Refresh Morph List", () => { SetMorphChooserChoices(true); }, color: UIColor.YELLOW));
      UI(UIBuilder.CreateStringChooserKeyVal(ref morphChooserStorable, UIColumn.RIGHT, "Select Morph", filterable: true, noDefaultSelection: true));
      SetMorphChooserChoices();
      UI(UIBuilder.CreateButton(UIColumn.RIGHT, "Add Morph", HandleAddMorph));
      foreach (DAZMorph morph in activeLayer.trackedMorphs)
      {
        UI(UIBuilder.CreateLabelWithX(UIColumn.RIGHT, Helpers.GetStandardMorphName(morph), () => { HandleDeleteMorph(morph); }));
      }


      CreateBottomPadding();
    }

    void HandleNewLayer()
    {
      Layer layer = new Layer();
      RefreshLayerList();
      activeLayerIdStorable.val = layer.id;
    }

    void HandleDuplicateLayer()
    {
      if (activeLayer == null) return;
      Layer layer = activeLayer.Clone();
      RefreshLayerList();
      activeLayerIdStorable.val = layer.id;
    }

    void HandleRenameLayer(string val)
    {
      if (activeLayer == null) return;
      activeLayer.name = val;
      RefreshLayerList();
      activeLayerIdStorable.val = activeLayer.id;
    }

    void HandleDeleteLayer()
    {
      if (activeLayer == null) return;
      activeLayer.Delete();
      RefreshLayerList();
      Utils.SelectStringChooserFirstValue(activeLayerIdStorable);
    }

    void HandleSelectAllControllers(bool pos, bool rot)
    {
      foreach (TrackedController tc in activeLayer.trackedControllers)
      {
        if (pos)
        {
          tc.trackPositionStorable.val = true;
        }
        if (rot)
        {
          tc.trackRotationStorable.val = true;
        }
      }
    }

    void HandleDeselectAllControllers()
    {
      foreach (TrackedController tc in activeLayer.trackedControllers)
      {
        tc.trackPositionStorable.val = false;
        tc.trackRotationStorable.val = false;
      }
    }

    void HandleAddMorph()
    {
      if (activeLayer == null || morphChooserStorable == null) return;
      DAZMorph morph = geometry.morphsControlUI.GetMorphByUid(morphChooserStorable.val);
      if (morph == null) return;
      if (activeLayer.trackedMorphs.Contains(morph)) return;
      activeLayer.trackedMorphs.Add(morph);
      activeLayer.trackedMorphs.Sort((a, b) => String.Compare(Helpers.GetStandardMorphName(a), Helpers.GetStandardMorphName(b)));

      morphChooserStorable.valNoCallback = "";
      InvalidateUI();
    }

    void HandleDeleteMorph(DAZMorph morph)
    {
      if (activeLayer == null) return;
      activeLayer.trackedMorphs.Remove(morph);
      InvalidateUI();
    }

    void HandleToggleMorphChooserFavorites(bool val)
    {
      SetMorphChooserChoices();
    }

    List<string> cachedMorphChoices = null;
    List<string> cachedMorphDisplayChoices = null;
    List<string> cachedFavoriteMorphChoices = null;
    List<string> cachedFavoriteMorphDisplayChoices = null;
    bool? cachedUseFavorites = null;
    void SetMorphChooserChoices(bool forceRefresh = false)
    {
      if (morphChooserStorable == null || morphChooserUseFavoritesStorable == null) return;
      bool needsInitialized = cachedMorphChoices == null || cachedMorphDisplayChoices == null || cachedFavoriteMorphChoices == null || cachedFavoriteMorphDisplayChoices == null;
      if (needsInitialized || forceRefresh)
      {
        cachedUseFavorites = null;
        cachedMorphChoices = new List<string>();
        cachedMorphDisplayChoices = new List<string>();
        cachedFavoriteMorphChoices = new List<string>();
        cachedFavoriteMorphDisplayChoices = new List<string>();

        List<KeyValuePair<string, string>> morphKeyValues = new List<KeyValuePair<string, string>>();
        List<KeyValuePair<string, string>> favoriteMorphKeyValues = new List<KeyValuePair<string, string>>();
        List<DAZMorph> morphs = geometry.morphsControlUI.GetMorphs();
        foreach (DAZMorph morph in morphs)
        {
          string uid = morph.uid;
          string name = Helpers.GetStandardMorphName(morph);
          KeyValuePair<string, string> entry = new KeyValuePair<string, string>(uid, name);
          morphKeyValues.Add(entry);
          if (morph.favorite)
          {
            favoriteMorphKeyValues.Add(entry);
          }
        }
        morphKeyValues.Sort((a, b) => a.Value.CompareTo(b.Value));
        favoriteMorphKeyValues.Sort((a, b) => a.Value.CompareTo(b.Value));
        foreach (KeyValuePair<string, string> entry in morphKeyValues)
        {
          cachedMorphChoices.Add(entry.Key);
          cachedMorphDisplayChoices.Add(entry.Value);
        }
        foreach (KeyValuePair<string, string> entry in favoriteMorphKeyValues)
        {
          cachedFavoriteMorphChoices.Add(entry.Key);
          cachedFavoriteMorphDisplayChoices.Add(entry.Value);
        }
      }

      bool needsRefresh = morphChooserUseFavoritesStorable.val != cachedUseFavorites;
      if (needsRefresh)
      {
        bool useFavorites = morphChooserUseFavoritesStorable.val;
        cachedUseFavorites = useFavorites;
        morphChooserStorable.choices = useFavorites ? cachedFavoriteMorphChoices : cachedMorphChoices;
        morphChooserStorable.displayChoices = useFavorites ? cachedFavoriteMorphDisplayChoices : cachedMorphDisplayChoices;
      }
    }


    // ================================================================================ //
    // ================================ ANIMATIONS TAB ================================ //
    // ================================================================================ //
    void BuildAnimationsTabUI()
    {
      CreateMainHeader(UIColumn.LEFT, "Animations");
      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"An <b>Animation</b> defines how a <b>Layer</b> should evolve over time. An animation is composed of one or more <b>Keyframes</b> connected by tweens.",
        185f
      ));

      if (activeLayer == null)
      {
        UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 50f));
        UI(UIBuilder.CreateInfoTextNoScroll(
          UIColumn.RIGHT,
          @"You must select a <b>Layer</b> before you can create any <b>Animations</b>.",
          2
        ));
        return;
      }

      CreateBasicFunctionsUI(
        "Anim.",
        activeAnimation == null,
        () => { UI(UIBuilder.CreateOnelineTextInput(ref editAnimationNameStorable, UIColumn.RIGHT, "Name", activeAnimation?.name ?? "", callback: HandleRenameAnimation)); },
        HandleNewAnimation,
        HandleDuplicateAnimation,
        HandleDeleteAnimation
      );


      CreateBottomPadding();
    }

    void HandleNewAnimation()
    {
      if (activeLayer == null) return;
      Animation animation = new Animation(activeLayer);
      RefreshAnimationList();
      activeAnimationIdStorable.val = animation.id;
    }

    void HandleDuplicateAnimation()
    {
      if (activeAnimation == null) return;
      Animation animation = activeAnimation.Clone();
      RefreshAnimationList();
      activeAnimationIdStorable.val = animation.id;
    }

    void HandleRenameAnimation(string val)
    {
      if (activeAnimation == null) return;
      activeAnimation.name = val;
      RefreshAnimationList();
      activeAnimationIdStorable.val = activeAnimation.id;
    }

    void HandleDeleteAnimation()
    {
      if (activeAnimation == null) return;
      activeAnimation.Delete();
      RefreshAnimationList();
      Utils.SelectStringChooserFirstValue(activeAnimationIdStorable);
    }


    // =============================================================================== //
    // ================================ KEYFRAMES TAB ================================ //
    // =============================================================================== //
    Animation.Keyframe activeKeyframe;

    void BuildKeyframesTabUI()
    {
      CreateMainHeader(UIColumn.LEFT, "Keyframes");
      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"A <b>Keyframe</b> is a snapshot of a <b>Layer</b>'s state that records morph and controller values. An <b>Animation</b> is composed of one or more keyframes.",
        185f
      ));

      if (activeAnimation == null)
      {
        UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 50f));
        UI(UIBuilder.CreateInfoTextNoScroll(
          UIColumn.RIGHT,
          @"You must select an <b>Animation</b> before you can create any <b>Keyframes</b>.",
          2
        ));
        return;
      }

      EnsureSelectedKeyframe();

      // KEYFRAME SELECTOR
      CreateSubHeader(UIColumn.LEFT, "Keyframe Selector");
      if (activeAnimation.keyframes.Count == 0)
      {
        UI(UIBuilder.CreateButton(UIColumn.LEFT, "New Keyframe", HandleAddKeyframeStart));
        return;
      }
      UI(UIBuilder.CreateButtonPair(UIColumn.LEFT, "<< New Keyframe", HandleAddKeyframeStart, "New Keyframe >>", HandleAddKeyframeEnd));
      UI(UIBuilder.CreateButtonPair(UIColumn.LEFT, "< New Keyframe", HandleAddKeyframeAfter, "New Keyframe >", HandleAddKeyframeBefore));
      UI(UIBuilder.CreateButton(UIColumn.LEFT, "Duplicate Keyframe", HandleDuplicateKeyframe));
      UI(UIBuilder.CreateButtonPair(UIColumn.LEFT, "< Move Keyframe", () => { HandleMoveKeyframe(-1); }, "Move Keyframe >", () => { HandleMoveKeyframe(1); }));
      UI(CreateKeyframeSelector(activeAnimation.keyframes, activeKeyframe, UIColumn.LEFT, HandleSelectKeyframe));

      if (activeKeyframe == null) return;

      UI(UIBuilder.CreateColorPickerFromStorable(activeKeyframe.colorStorable, UIColumn.LEFT));
      UI(UIBuilder.CreateButton(UIColumn.LEFT, "Apply Color", () => { InvalidateUI(); }));

      // ACTIONS
      UI(UIBuilder.CreateSpacer(UIColumn.LEFT));
      CreateSubHeader(UIColumn.LEFT, "Actions");
      // TODO: ACTIONS
      UI(UIBuilder.CreateButton(UIColumn.LEFT, "On Enter Keyframe", null));
      UI(UIBuilder.CreateButton(UIColumn.LEFT, "On Keyframe Playing", null));
      UI(UIBuilder.CreateButton(UIColumn.LEFT, "On Exit Keyframe", null));

      // KEYFRAME DETAILS
      CreateSubHeader(UIColumn.RIGHT, "Keyframe Details");
      UI(UIBuilder.CreateButton(UIColumn.RIGHT, "Capture Current State", HandleCaptureKeyframe, color: UIColor.YELLOW));
      UI(UIBuilder.CreateButton(UIColumn.RIGHT, "Go To Keyframe", HandleCaptureKeyframe));
      UI(UIBuilder.CreateSpacer(UIColumn.RIGHT));
      UI(UIBuilder.CreateButton(UIColumn.RIGHT, "Delete Keyframe", HandleDeleteKeyframe, color: UIColor.RED));
      UI(UIBuilder.CreateSpacer(UIColumn.RIGHT));

      UI(UIBuilder.CreateSliderFromStorable(activeKeyframe.durationStorable, UIColumn.RIGHT, 5f, 0f, 10f));
      UI(UIBuilder.CreateStringChooserFromStorable(activeKeyframe.easingStorable, UIColumn.RIGHT));
      UI(UIBuilder.CreateSpacer(UIColumn.RIGHT));

      // MORPHS
      CreateSubHeader(UIColumn.RIGHT, "Morph Captures");
      foreach (DAZMorph morph in activeKeyframe.animation.layer.trackedMorphs)
      {
        CapturedMorph capture = activeKeyframe.GetCapturedMorph(morph.uid);
        if (morph.jsonFloat.min >= 0f) morph.jsonFloat.min = -1f;
        if (morph.jsonFloat.max <= 0f) morph.jsonFloat.max = 1f;
        UI(UIBuilder.CreateSliderFromStorable(morph.jsonFloat, UIColumn.RIGHT, morph.jsonFloat.defaultVal, -1f, 1f));
        string str = capture?.value == null ? "<b>Val</b>: <NO DATA>" : $"<b>Val</b>: {capture.value}";
        UI(UIBuilder.CreateInfoTextNoScroll(UIColumn.RIGHT, $"<size=24>{str}</size>", 1, background: false));
      }
      if (activeKeyframe.animation.layer.trackedMorphs.Count == 0)
      {
        UI(UIBuilder.CreateInfoTextNoScroll(UIColumn.RIGHT, "<none>", 1, background: false));
      }
      UI(UIBuilder.CreateSpacer(UIColumn.RIGHT));

      // CONTROLLERS
      CreateSubHeader(UIColumn.RIGHT, "Controller Captures");
      int controllerCount = 0;
      foreach (TrackedController tc in activeKeyframe.animation.layer.trackedControllers)
      {
        if (!tc.isTracked) continue;
        controllerCount++;
        CapturedController capture = activeKeyframe.GetCapturedController(tc.controller.name);
        string nameStr = tc.controller.name;
        string posStr = "";
        string rotStr = "";
        string joinStr = "";
        if (tc.trackPositionStorable.val)
        {
          posStr = capture?.position == null ? "<b>P</b>: <NO DATA>" : $"<b>P</b>: {capture.position.Value}";
        }
        if (tc.trackRotationStorable.val)
        {
          rotStr = capture?.rotation == null ? "<b>R</b>: <NO DATA>" : $"<b>R</b>: {capture.rotation.Value.eulerAngles}";
        }
        if (posStr.Length > 0 && rotStr.Length > 0)
        {
          joinStr = " ";
        }
        string str = $"<b>{nameStr}</b>\n<size=24>{posStr}{joinStr}{rotStr}</size>";
        UI(UIBuilder.CreateInfoTextNoScroll(UIColumn.RIGHT, str, 2, background: false));
      }
      if (controllerCount == 0)
      {
        UI(UIBuilder.CreateInfoTextNoScroll(UIColumn.RIGHT, "<none>", 1, background: false));
      }


      CreateBottomPadding();
    }

    int? GetActiveKeyframeIndex()
    {
      if (activeAnimation == null || activeKeyframe == null) return null;
      int index = activeAnimation.keyframes.FindIndex((k) => k.id == activeKeyframe.id);
      if (index == -1) return null;
      return index;
    }

    void HandleSelectKeyframe(Animation.Keyframe keyframe)
    {
      activeKeyframe = keyframe;
      InvalidateUI();
    }

    void EnsureSelectedKeyframe()
    {
      if (activeAnimation == null)
      {
        if (activeKeyframe != null)
        {
          HandleSelectKeyframe(null);
        }
        return;
      }
      if (activeAnimation.keyframes.Count == 0)
      {
        if (activeKeyframe != null)
        {
          HandleSelectKeyframe(null);
        }
        return;
      }
      if (!activeAnimation.keyframes.Contains(activeKeyframe))
      {
        HandleSelectKeyframe(activeAnimation.keyframes[0]);
        return;
      }
    }

    void HandleAddKeyframeStart()
    {
      if (activeAnimation == null) return;
      Animation.Keyframe keyframe = new Animation.Keyframe(activeAnimation, 0);
      HandleSelectKeyframe(keyframe);
      InvalidateUI();
    }

    void HandleAddKeyframeEnd()
    {
      if (activeAnimation == null) return;
      Animation.Keyframe keyframe = new Animation.Keyframe(activeAnimation, -1);
      HandleSelectKeyframe(keyframe);
      InvalidateUI();
    }

    void HandleAddKeyframeBefore()
    {
      int? index = GetActiveKeyframeIndex();
      if (activeAnimation == null || index == null) return;
      Animation.Keyframe keyframe = new Animation.Keyframe(activeAnimation, index.Value);
      HandleSelectKeyframe(keyframe);
      InvalidateUI();
    }

    void HandleAddKeyframeAfter()
    {
      int? index = GetActiveKeyframeIndex();
      if (activeAnimation == null || index == null) return;
      Animation.Keyframe keyframe = new Animation.Keyframe(activeAnimation, index.Value + 1);
      HandleSelectKeyframe(keyframe);
      InvalidateUI();
    }

    void HandleMoveKeyframe(int direction)
    {
      int? index = GetActiveKeyframeIndex();
      if (activeAnimation == null || activeKeyframe == null || index == null) return;
      int oldIndex = index.Value;
      int newIndex = index.Value + direction;
      if (newIndex < 0 || newIndex >= activeAnimation.keyframes.Count) return;
      Animation.Keyframe temp = activeAnimation.keyframes[newIndex];
      activeAnimation.keyframes[newIndex] = activeKeyframe;
      activeAnimation.keyframes[oldIndex] = temp;
      InvalidateUI();
    }

    void HandleDuplicateKeyframe()
    {
      int? index = GetActiveKeyframeIndex();
      if (activeKeyframe == null || index == null) return;
      Animation.Keyframe keyframe = activeKeyframe.Clone(index: index.Value + 1);
      HandleSelectKeyframe(keyframe);
      InvalidateUI();
    }

    void HandleDeleteKeyframe()
    {
      if (activeKeyframe == null) return;
      activeKeyframe.Delete();
      InvalidateUI();
    }

    void HandleCaptureKeyframe()
    {
      if (activeKeyframe == null) return;
      activeKeyframe.CaptureLayerState();
      InvalidateUI();
    }


    // ================================================================================= //
    // ================================ TRANSITIONS TAB ================================ //
    // ================================================================================= //
    void BuildTransitionsTabUI()
    {
      CreateMainHeader(UIColumn.LEFT, "Transitions");
      UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 45f));

      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"A <b>Transition</b> defines how to move from one animation to another. The default transition is a simple tween, but if more control is needed <b>Keyframes</b> may be added for precision.",
        5
      ));


      CreateBottomPadding();
    }


    // ============================================================================== //
    // ================================ TRIGGERS TAB ================================ //
    // ============================================================================== //
    void BuildTriggersTabUI()
    {
      CreateMainHeader(UIColumn.LEFT, "Triggers");
      UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 45f));

      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"A <b>Trigger</b> is a custom event that can be called from other atoms in your scene using the 'Call Trigger' action. Triggers allow external management of the character's state.",
        5
      ));


      CreateBottomPadding();
    }


    // ================================================================================ //
    // ================================ CUSTOM PREFABS ================================ //
    // ================================================================================ //
    // ====== Controller Selector ====== //
    public class UIDynamicControllerSelector : UIDynamicBase
    {
      public Text label;
      public Toggle posToggle;
      public Toggle rotToggle;
    }

    private GameObject controllerSelectorPrefab;
    public UIDynamicControllerSelector CreateControllerSelector(TrackedController tc, UIColumn side, TrackedController.SetValueCallback callback = null)
    {
      if (controllerSelectorPrefab == null)
      {
        const float bgSize = 105f;
        const float bgSpacing = 10f;
        const float labelOffsetX = 8f;
        const float labelOffsetY = 2f;
        const float checkSize = 43f;
        const float checkOffsetX = 4f;
        const float checkOffsetY = -3.5f;

        UIDynamicControllerSelector uid = UIBuilder.CreateUIDynamicPrefab<UIDynamicControllerSelector>("LabelWithToggle", 50f);
        controllerSelectorPrefab = uid.gameObject;

        RectTransform background = UIBuilder.InstantiateBackground(uid.transform);

        RectTransform labelRect = UIBuilder.InstantiateLabel(uid.transform);
        labelRect.offsetMin = new Vector2(5f, 0f);
        Text labelText = labelRect.GetComponent<Text>();
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.text = "";

        // == POS == //
        RectTransform posBackgroundRect = UIBuilder.InstantiateBackground(uid.transform);
        posBackgroundRect.anchorMin = new Vector2(1f, 0f);
        posBackgroundRect.anchorMax = new Vector2(1f, 1f);
        posBackgroundRect.offsetMin = new Vector2(-(2f * bgSize + bgSpacing), 0f);
        posBackgroundRect.offsetMax = new Vector2(-(bgSize + bgSpacing), 0f);
        Image posBackgroundImg = posBackgroundRect.GetComponent<Image>();
        posBackgroundImg.color = new Color(1f, 1f, 1f, 1f);

        RectTransform posToggleRect = UIBuilder.InstantiateToggle(posBackgroundRect, checkSize);
        posToggleRect.anchorMin = new Vector2(1f, 0f);
        posToggleRect.anchorMax = new Vector2(1f, 1f);
        posToggleRect.offsetMin = new Vector2(-checkSize - checkOffsetX, checkOffsetY);
        posToggleRect.offsetMax = new Vector2(-checkOffsetX, checkOffsetY);

        RectTransform posLabelRect = UIBuilder.InstantiateLabel(posBackgroundRect);
        posLabelRect.offsetMin = new Vector2(labelOffsetX, labelOffsetY);
        posLabelRect.offsetMax = new Vector2(labelOffsetX, labelOffsetY);
        Text posLabel = posLabelRect.GetComponent<Text>();
        posLabel.alignment = TextAnchor.MiddleLeft;
        posLabel.text = "<size=20><b>POS</b></size>";

        // == ROT == //
        RectTransform rotBackgroundRect = UIBuilder.InstantiateBackground(uid.transform);
        rotBackgroundRect.anchorMin = new Vector2(1f, 0f);
        rotBackgroundRect.anchorMax = new Vector2(1f, 1f);
        rotBackgroundRect.offsetMin = new Vector2(-bgSize, 0f);
        rotBackgroundRect.offsetMax = new Vector2(0f, 0f);
        Image rotBackgroundImg = rotBackgroundRect.GetComponent<Image>();
        rotBackgroundImg.color = new Color(1f, 1f, 1f, 1f);

        RectTransform rotToggleRect = UIBuilder.InstantiateToggle(rotBackgroundRect, checkSize);
        rotToggleRect.anchorMin = new Vector2(1f, 0f);
        rotToggleRect.anchorMax = new Vector2(1f, 1f);
        rotToggleRect.offsetMin = new Vector2(-checkSize - checkOffsetX, checkOffsetY);
        rotToggleRect.offsetMax = new Vector2(-checkOffsetX, checkOffsetY);

        RectTransform rotLabelRect = UIBuilder.InstantiateLabel(rotBackgroundRect);
        rotLabelRect.offsetMin = new Vector2(labelOffsetX, labelOffsetY);
        rotLabelRect.offsetMax = new Vector2(labelOffsetX, labelOffsetY);
        Text rotLabel = rotLabelRect.GetComponent<Text>();
        rotLabel.alignment = TextAnchor.MiddleLeft;
        rotLabel.text = "<size=20><b>ROT</b></size>";

        uid.label = labelText;
        uid.posToggle = posToggleRect.GetComponent<Toggle>();
        uid.rotToggle = rotToggleRect.GetComponent<Toggle>();
      }
      {
        if (callback != null)
        {
          tc.setCallbackFunction = callback;
        }
        Transform t = CreateUIElement(controllerSelectorPrefab.transform, side == UIColumn.RIGHT);
        UIDynamicControllerSelector uid = t.GetComponent<UIDynamicControllerSelector>();
        uid.label.text = tc.controller.name;
        tc.trackPositionStorable.RegisterToggle(uid.posToggle);
        tc.trackRotationStorable.RegisterToggle(uid.rotToggle);
        uid.gameObject.SetActive(true);
        return uid;
      }
    }


    // ====== Keyframe Selector ====== //
    public class UIDynamicKeyframeSelector : UIDynamicBase
    {
      public Animation.Keyframe activeKeyframe;
      public List<Animation.Keyframe> keyframes;
      public SelectKeyframeCallback callback;
      public RectTransform buttonContainer;

      private List<UIDynamicButton> buttons = new List<UIDynamicButton>();

      private const float buttonHeight = 70f;
      private const float buttonSpacing = 10f;

      void Start()
      {
        int rows = (keyframes.Count - 1) / 5 + 1;
        float totalHeight = rows * (buttonHeight + buttonSpacing) - buttonSpacing;
        layout.preferredHeight = layout.minHeight = totalHeight + 10f;

        for (int i = 0; i < keyframes.Count; i++)
        {
          Animation.Keyframe keyframe = keyframes[i];
          int x = i % 5;
          int y = i / 5;
          float yOffset = (buttonHeight + buttonSpacing) * y;

          RectTransform buttonRect = UIBuilder.InstantiateButton(buttonContainer);
          buttonRect.anchorMin = new Vector2(0.2f * x, 1f);
          buttonRect.anchorMax = new Vector2(0.2f * x + 0.2f, 1f);
          buttonRect.offsetMin = new Vector2(buttonSpacing / 2f, -buttonHeight - yOffset);
          buttonRect.offsetMax = new Vector2(-buttonSpacing / 2f, 0f - yOffset);
          UIDynamicButton button = buttonRect.GetComponent<UIDynamicButton>();
          button.buttonText.text = $"<size=35>{i + 1}</size>";
          if (keyframe.id == activeKeyframe?.id)
          {
            button.buttonText.text = $"<size=35>[ {i + 1} ]</size>";
          }
          HSVColor color = keyframe.colorStorable.val;
          button.buttonColor = Color.HSVToRGB(color.H, color.S, color.V);

          if (callback != null)
          {
            button.button.onClick.AddListener(() => { callback(keyframe); });
          }
          buttons.Add(button);
        }
      }

      void OnDestroy()
      {
        foreach (UIDynamicButton button in buttons)
        {
          GameObject.Destroy(button.gameObject);
        }
      }
    }
    public delegate void SelectKeyframeCallback(Animation.Keyframe keyframe);

    private GameObject keyframeSelectorPrefab;
    public UIDynamicKeyframeSelector CreateKeyframeSelector(List<Animation.Keyframe> keyframes, Animation.Keyframe activeKeyframe, UIColumn side, SelectKeyframeCallback callback = null)
    {
      if (keyframeSelectorPrefab == null)
      {
        UIDynamicKeyframeSelector uid = UIBuilder.CreateUIDynamicPrefab<UIDynamicKeyframeSelector>("KeyframeSelector", 0f);
        keyframeSelectorPrefab = uid.gameObject;
        RectTransform background = UIBuilder.InstantiateBackground(uid.transform);
        RectTransform buttonContainer = UIBuilder.InstantiateEmptyRect(uid.transform);
        buttonContainer.offsetMin = new Vector2(0f, 5f);
        buttonContainer.offsetMax = new Vector2(0f, -5f);
        uid.buttonContainer = buttonContainer;
      }
      {
        Transform t = CreateUIElement(keyframeSelectorPrefab.transform, side == UIColumn.RIGHT);
        UIDynamicKeyframeSelector uid = t.GetComponent<UIDynamicKeyframeSelector>();
        uid.activeKeyframe = activeKeyframe;
        uid.keyframes = keyframes;
        uid.callback = callback;
        uid.gameObject.SetActive(true);
        return uid;
      }
    }
  }
}
