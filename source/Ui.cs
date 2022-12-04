using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using VaMLib;

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
      public const string SendMessages = "Send Msgs";
      public const string ReceiveMessages = "Receive Msgs";

      public static readonly string[] list = new string[] { Info, Groups, States, Layers, Animations, Keyframes, Transitions, SendMessages, ReceiveMessages };
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
      VaMTrigger.Init(this);
      VaMUI.Init(this, CreateUIElement);
      RebuildUI();
    }

    void UIDestroy()
    {
      VaMUI.Destroy();
      VaMUtils.SafeDestroy(ref tabBarPrefab);
      VaMUtils.SafeDestroy(ref keyframeSelectorPrefab);
      VaMUtils.SafeDestroy(ref playlistEntryContainerPrefab);
      VaMTrigger.Destroy();
    }

    void UIUpdate()
    {
      if (uiNeedsRebuilt)
      {
        uiNeedsRebuilt = false;
        RebuildUI();
      }
    }

    void RequestRedraw()
    {
      uiNeedsRebuilt = true;
    }

    void Draw(object item)
    {
      uiItems.Add(item);
    }

    void RebuildUI()
    {
      VaMUI.RemoveUIElements(ref uiItems);

      Draw(VaMUI.CreateToggle(ref playbackEnabledStorable, VaMUI.LEFT, "Playback Enabled", true));
      Draw(VaMUI.CreateToggle(ref hideTopUIStorable, VaMUI.RIGHT, "Hide Top UI", false, callback: HandleHideTopUI));

      if (!hideTopUIStorable.val)
      {
        Draw(VaMUI.CreateStringChooserKeyVal(ref activeGroupIdStorable, VaMUI.LEFT, "Group", callback: HandleSelectGroup));
        Draw(VaMUI.CreateStringChooserKeyVal(ref activeStateIdStorable, VaMUI.RIGHT, "State", callback: HandleSelectState));
        Draw(VaMUI.CreateStringChooserKeyVal(ref activeLayerIdStorable, VaMUI.LEFT, "Layer", callback: HandleSelectLayer));
        Draw(VaMUI.CreateStringChooserKeyVal(ref activeAnimationIdStorable, VaMUI.RIGHT, "Animation", callback: HandleSelectAnimation));

        Draw(VaMUI.CreateTabBar(ref tabBarPrefab, VaMUI.LEFT, Tabs.list, HandleTabSelect, 5));
        Draw(VaMUI.CreateSpacer(VaMUI.RIGHT, 2 * 55f));
      }

      BuildActiveTabUI();
    }

    void HandleHideTopUI(bool val)
    {
      RequestRedraw();
    }

    void HandleTabSelect(string tabName)
    {
      activeTab = tabName;
      RequestRedraw();
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
        case Tabs.SendMessages:
          BuildSendMessagesTabUI();
          break;
        case Tabs.ReceiveMessages:
          BuildReceiveMessagesTabUI();
          break;
        default:
          BuildInfoTabUI();
          break;
      }
    }

    void HandleSelectGroup(string val)
    {
      VaMUtils.EnsureStringChooserValue(activeGroupIdStorable, defaultToFirstChoice: true, noCallback: true);
      activeGroup = Group.list.Find((g) => g.id == activeGroupIdStorable.val);

      RefreshStateList();
      VaMUtils.SelectStringChooserFirstValue(activeStateIdStorable);

      if (editGroupNameStorable != null)
      {
        editGroupNameStorable.valNoCallback = activeGroup?.name ?? "";
      }

      RequestRedraw();
    }

    void HandleSelectState(string val)
    {
      VaMUtils.EnsureStringChooserValue(activeStateIdStorable, defaultToFirstChoice: true, noCallback: true);
      activeState = activeGroup?.states.Find((s) => s.id == activeStateIdStorable.val);

      if (editStateNameStorable != null)
      {
        editStateNameStorable.valNoCallback = activeState?.name ?? "";
      }

      RequestRedraw();
    }

    void HandleSelectLayer(string val)
    {
      VaMUtils.EnsureStringChooserValue(activeLayerIdStorable, defaultToFirstChoice: true, noCallback: true);
      activeLayer = Layer.list.Find((l) => l.id == activeLayerIdStorable.val);

      RefreshAnimationList();
      VaMUtils.SelectStringChooserFirstValue(activeAnimationIdStorable);

      if (editLayerNameStorable != null)
      {
        editLayerNameStorable.valNoCallback = activeLayer?.name ?? "";
      }

      RequestRedraw();
    }

    void HandleSelectAnimation(string val)
    {
      VaMUtils.EnsureStringChooserValue(activeAnimationIdStorable, defaultToFirstChoice: true, noCallback: true);
      activeAnimation = activeLayer?.animations.Find((a) => a.id == activeAnimationIdStorable.val);

      if (editAnimationNameStorable != null)
      {
        editAnimationNameStorable.valNoCallback = activeAnimation?.name ?? "";
      }

      RequestRedraw();
    }

    void RefreshGroupList()
    {
      List<KeyValuePair<string, string>> entries = new List<KeyValuePair<string, string>>();
      foreach (Group group in Group.list)
      {
        entries.Add(new KeyValuePair<string, string>(group.id, group.name));
      }
      entries.Sort((a, b) => a.Value.CompareTo(b.Value));
      VaMUtils.SetStringChooserChoices(activeGroupIdStorable, entries);

      RequestRedraw();
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
      VaMUtils.SetStringChooserChoices(activeStateIdStorable, entries);

      RequestRedraw();
    }

    void RefreshLayerList()
    {
      List<KeyValuePair<string, string>> entries = new List<KeyValuePair<string, string>>();
      foreach (Layer layer in Layer.list)
      {
        entries.Add(new KeyValuePair<string, string>(layer.id, layer.name));
      }
      entries.Sort((a, b) => a.Value.CompareTo(b.Value));
      VaMUtils.SetStringChooserChoices(activeLayerIdStorable, entries);

      RequestRedraw();
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
      VaMUtils.SetStringChooserChoices(activeAnimationIdStorable, entries);

      RequestRedraw();
    }

    void CreateMainHeader(VaMUI.Column side, string text)
    {
      Draw(VaMUI.CreateHeaderText(side, text, 50f));
    }

    void CreateSubHeader(VaMUI.Column side, string text)
    {
      Draw(VaMUI.CreateHeaderText(side, text, 38f));
    }

    void CreateBasicFunctionsUI(string category, bool showNewOnly, Action createNameInput, UnityAction newHandler, UnityAction duplicateHandler, UnityAction deleteHandler)
    {
      if (showNewOnly)
      {
        Draw(VaMUI.CreateSpacer(VaMUI.RIGHT, 50f));
        Draw(VaMUI.CreateButton(VaMUI.RIGHT, $"New {category}", newHandler, color: VaMUI.GREEN));
      }
      else
      {
        createNameInput();
        Draw(VaMUI.CreateButtonPair(VaMUI.RIGHT, $"New {category}", newHandler, $"Duplicate {category}", duplicateHandler, VaMUI.GREEN, VaMUI.BLUE));
        Draw(VaMUI.CreateSpacer(VaMUI.RIGHT));
        Draw(VaMUI.CreateButton(VaMUI.RIGHT, $"Delete {category}", deleteHandler, color: VaMUI.RED));
        Draw(VaMUI.CreateSpacer(VaMUI.RIGHT));
      }
    }

    void CreateBottomPadding()
    {
      Draw(VaMUI.CreateSpacer(VaMUI.LEFT, 100f));
      Draw(VaMUI.CreateSpacer(VaMUI.RIGHT, 100f));
    }


    // ========================================================================== //
    // ================================ INFO TAB ================================ //
    // ========================================================================== //
    void BuildInfoTabUI()
    {
      CreateMainHeader(VaMUI.LEFT, "Info");
      Draw(VaMUI.CreateSpacer(VaMUI.RIGHT, 45f));

      Draw(VaMUI.CreateInfoTextNoScroll(
        VaMUI.LEFT,
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
      CreateMainHeader(VaMUI.LEFT, "Groups");
      Draw(VaMUI.CreateInfoTextNoScroll(
        VaMUI.LEFT,
        @"A <b>Group</b> is a collection of <b>States</b> that are independent of other groups. Only one state can be active per group. For example, you might use one group for walking and one group for gestures.",
        185f
      ));

      CreateBasicFunctionsUI(
        "Group",
        activeGroup == null,
        () => { Draw(VaMUI.CreateOnelineTextInput(ref editGroupNameStorable, VaMUI.RIGHT, "Name", activeGroup?.name ?? "", callback: HandleRenameGroup)); },
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
      VaMUtils.SelectStringChooserFirstValue(activeGroupIdStorable);
    }


    // ============================================================================ //
    // ================================ STATES TAB ================================ //
    // ============================================================================ //
    AnimationPlaylist activePlaylist;

    void BuildStatesTabUI()
    {
      SetActivePlaylist();

      CreateMainHeader(VaMUI.LEFT, "States");
      Draw(VaMUI.CreateInfoTextNoScroll(
        VaMUI.LEFT,
        @"A <b>State</b> defines what a character is currently doing (idle, sitting, etc). A state assigns <b>Animations</b> to <b>Layers</b> that can be played either sequentially or randomly.",
        185f
      ));

      if (activeGroup == null)
      {
        Draw(VaMUI.CreateSpacer(VaMUI.RIGHT, 50f));
        Draw(VaMUI.CreateInfoTextNoScroll(
          VaMUI.RIGHT,
          @"You must select a <b>Group</b> before you can create any <b>States</b>.",
          2
        ));
        return;
      }

      CreateBasicFunctionsUI(
        "State",
        activeState == null,
        () => { Draw(VaMUI.CreateOnelineTextInput(ref editStateNameStorable, VaMUI.RIGHT, "Name", activeState?.name ?? "", callback: HandleRenameState)); },
        HandleNewState,
        HandleDuplicateState,
        HandleDeleteState
      );

      if (activeState == null) return;

      CreateSubHeader(VaMUI.LEFT, "Target Layers");
      Draw(VaMUI.CreateInfoTextNoScroll(
        VaMUI.LEFT,
        @"You can only assign animations to layers in this list.",
        2
      ));

      if (activeLayer == null)
      {
        Draw(VaMUI.CreateInfoTextNoScroll(
          VaMUI.LEFT,
          @"You must create a <b>Layer</b> before you can edit this <b>State</b>.",
          2
        ));
        return;
      }

      // LAYERS
      Draw(VaMUI.CreateButton(VaMUI.LEFT, "Add Current Layer", HandleCreatePlaylist));
      foreach (AnimationPlaylist playlist in activeState.playlists)
      {
        bool isActive = playlist.layer == activeLayer;
        string label = isActive ? $"[ {playlist.layer.name} ]" : playlist.layer.name;
        Draw(VaMUI.CreateLabelWithX(VaMUI.LEFT, label, () => { HandleDeletePlaylist(playlist); }));
      }

      CreateSubHeader(VaMUI.RIGHT, "Animation Playlist");
      if (activePlaylist == null)
      {
        Draw(VaMUI.CreateInfoTextNoScroll(
          VaMUI.RIGHT,
          @"Select or add a <b>Layer</b> to the list to edit its animation playlist.",
          2
        ));
        return;
      }

      if (activeAnimation == null)
      {
        Draw(VaMUI.CreateInfoTextNoScroll(
          VaMUI.RIGHT,
          @"There are no <b>Animations</b> available for this <b>Layer</b>.",
          2
        ));
        return;
      }

      // OPTIONS
      Draw(VaMUI.CreateSpacer(VaMUI.LEFT));
      CreateSubHeader(VaMUI.LEFT, "Playlist Options");
      Draw(VaMUI.CreateStringChooserFromStorable(activePlaylist.modeStorable, VaMUI.LEFT));
      activePlaylist.modeStorable.setCallbackFunction = (string val) => { RequestRedraw(); };
      if (activePlaylist.modeStorable.val == PlaylistMode.Sequential)
      {
        Draw(VaMUI.CreateInfoTextNoScroll(
          VaMUI.LEFT,
          @"The animations will play in order.",
          1
        ));
      }
      else if (activePlaylist.modeStorable.val == PlaylistMode.Random)
      {
        Draw(VaMUI.CreateInfoTextNoScroll(
          VaMUI.LEFT,
          @"The animations will play randomly.",
          1
        ));
      }

      // DEFAULTS
      Draw(VaMUI.CreateSpacer(VaMUI.LEFT));
      CreateSubHeader(VaMUI.LEFT, "Defaults");
      Draw(VaMUI.CreateInfoTextNoScroll(
        VaMUI.LEFT,
        @"New animations will use these defaults.",
        1
      ));
      Draw(VaMUI.CreateStringChooserFromStorable(activePlaylist.defaultTimingModeStorable, VaMUI.LEFT));
      activePlaylist.defaultTimingModeStorable.setCallbackFunction = (string val) => { RequestRedraw(); };
      Draw(VaMUI.CreateSliderFromStorable(activePlaylist.defaultWeightStorable, VaMUI.LEFT, 0.5f, 0f, 1f));
      if (activePlaylist.defaultTimingModeStorable.val == TimingMode.RandomDuration)
      {
        Draw(VaMUI.CreateSliderFromStorable(activePlaylist.defaultDurationMinStorable, VaMUI.LEFT, 10f, 0f, 30f));
        Draw(VaMUI.CreateSliderFromStorable(activePlaylist.defaultDurationMaxStorable, VaMUI.LEFT, 10f, 0f, 30f));
      }
      else
      {
        Draw(VaMUI.CreateSliderFromStorable(activePlaylist.defaultDurationFixedStorable, VaMUI.LEFT, 10f, 0f, 30f));
      }
      Draw(VaMUI.CreateButton(VaMUI.LEFT, "Apply to All", HandleApplyDefaultsToAll));

      // ACTIONS
      Draw(VaMUI.CreateSpacer(VaMUI.LEFT));
      CreateSubHeader(VaMUI.LEFT, "Actions");
      Draw(VaMUI.CreateButton(VaMUI.LEFT, "On Enter State", activeState.onEnterTrigger.OpenPanel));
      Draw(VaMUI.CreateButton(VaMUI.LEFT, "On Exit State", activeState.onExitTrigger.OpenPanel));

      // PLAYLIST
      Draw(VaMUI.CreateInfoTextNoScroll(
        VaMUI.RIGHT,
        @"This is the playlist of animations for this layer.",
        2
      ));

      Draw(VaMUI.CreateButton(VaMUI.RIGHT, "Add Current Animation", HandleAddPlaylistEntry));
      Draw(VaMUI.CreateSpacer(VaMUI.RIGHT));

      for (int i = 0; i < activePlaylist.entries.Count; i++)
      {
        PlaylistEntry entry = activePlaylist.entries[i];
        Draw(CreatePlaylistEntryContainer(activePlaylist.modeStorable.val, entry.timingModeStorable.val, i + 1, VaMUI.RIGHT));
        Draw(VaMUI.CreateSpacer(VaMUI.RIGHT, 10f));
        Draw(VaMUI.CreateLabelWithX(VaMUI.RIGHT, $"<b>{entry.animation.name}</b>", () => { HandleDeletePlaylistEntry(entry); }));
        Draw(VaMUI.CreateButtonPair(VaMUI.RIGHT, "Move Up", () => HandleMovePlaylistEntry(entry, -1), "Move Down", () => HandleMovePlaylistEntry(entry, 1)));
        Draw(VaMUI.CreateStringChooserFromStorable(entry.timingModeStorable, VaMUI.RIGHT));
        entry.timingModeStorable.setCallbackFunction = (string val) => { RequestRedraw(); };
        if (activePlaylist.modeStorable.val == PlaylistMode.Random)
        {
          Draw(VaMUI.CreateSliderFromStorable(entry.weightStorable, VaMUI.RIGHT, 0.5f, 0f, 1f));
        }
        if (entry.timingModeStorable.val == TimingMode.FixedDuration)
        {
          Draw(VaMUI.CreateSliderFromStorable(entry.durationFixedStorable, VaMUI.RIGHT, 10f, 0f, 30f));
        }
        else if (entry.timingModeStorable.val == TimingMode.RandomDuration)
        {
          Draw(VaMUI.CreateSliderFromStorable(entry.durationMinStorable, VaMUI.RIGHT, 10f, 0f, 30f));
          Draw(VaMUI.CreateSliderFromStorable(entry.durationMaxStorable, VaMUI.RIGHT, 10f, 0f, 30f));
        }
        Draw(VaMUI.CreateSpacer(VaMUI.RIGHT));
      }


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
      VaMUtils.SelectStringChooserFirstValue(activeStateIdStorable);
    }

    void SetActivePlaylist()
    {
      if (activeState == null || activeLayer == null)
      {
        activePlaylist = null;
        return;
      }
      activePlaylist = activeState.GetPlaylist(activeLayer);
    }

    void HandleCreatePlaylist()
    {
      if (activeState == null) return;
      activeState.CreatePlaylist(activeLayer);

      RequestRedraw();
    }

    void HandleDeletePlaylist(AnimationPlaylist playlist)
    {
      if (activeState == null) return;
      activeState.playlists.Remove(playlist);

      RequestRedraw();
    }

    void HandleAddPlaylistEntry()
    {
      if (activePlaylist == null || activeAnimation == null) return;
      activePlaylist.AddEntry(activeAnimation);

      RequestRedraw();
    }

    void HandleDeletePlaylistEntry(PlaylistEntry entry)
    {
      if (activePlaylist == null) return;
      activePlaylist.entries.Remove(entry);

      RequestRedraw();
    }

    void HandleMovePlaylistEntry(PlaylistEntry entry, int direction)
    {
      if (activePlaylist == null) return;
      int index = activePlaylist.entries.IndexOf(entry);
      if (index < 0) return;
      int oldIndex = index;
      int newIndex = index + direction;
      if (newIndex < 0 || newIndex >= activePlaylist.entries.Count) return;
      PlaylistEntry temp = activePlaylist.entries[newIndex];
      activePlaylist.entries[newIndex] = entry;
      activePlaylist.entries[oldIndex] = temp;
      RequestRedraw();
    }

    void HandleApplyDefaultsToAll()
    {
      if (activePlaylist == null) return;
      foreach (PlaylistEntry entry in activePlaylist.entries)
      {
        entry.SetFromDefaults();
      }
    }


    // ============================================================================ //
    // ================================ LAYERS TAB ================================ //
    // ============================================================================ //
    JSONStorableStringChooser morphChooserStorable;
    JSONStorableBool morphChooserUseFavoritesStorable;

    void BuildLayersTabUI()
    {
      CreateMainHeader(VaMUI.LEFT, "Layers");
      Draw(VaMUI.CreateInfoTextNoScroll(
        VaMUI.LEFT,
        @"A <b>Layer</b> is a set of controllers and/or morphs that are acted upon by an <b>Animation</b>. Layers allow a character to have several independently animated parts.",
        185f
      ));

      CreateBasicFunctionsUI(
        "Layer",
        activeLayer == null,
        () => { Draw(VaMUI.CreateOnelineTextInput(ref editLayerNameStorable, VaMUI.RIGHT, "Name", activeLayer?.name ?? "", callback: HandleRenameLayer)); },
        HandleNewLayer,
        HandleDuplicateLayer,
        HandleDeleteLayer
      );

      if (activeLayer == null) return;

      CreateSubHeader(VaMUI.LEFT, "Controllers");
      Draw(VaMUI.CreateButtonPair(VaMUI.LEFT, "Select All Position", () => { HandleSelectAllControllers(true, false); }, "Select All Rotation", () => { HandleSelectAllControllers(false, true); }));
      Draw(VaMUI.CreateButton(VaMUI.LEFT, "Deselect All", HandleDeselectAllControllers));
      foreach (TrackedController tc in activeLayer.trackedControllers)
      {
        Draw(CreateControllerSelector(tc, VaMUI.LEFT));
      }

      CreateSubHeader(VaMUI.RIGHT, "Morphs");
      Draw(VaMUI.CreateToggle(ref morphChooserUseFavoritesStorable, VaMUI.RIGHT, "Favorites Only", true, callback: HandleToggleMorphChooserFavorites));
      Draw(VaMUI.CreateButton(VaMUI.RIGHT, "Force Refresh Morph List", () => { SetMorphChooserChoices(true); }, color: VaMUI.YELLOW));
      Draw(VaMUI.CreateStringChooserKeyVal(ref morphChooserStorable, VaMUI.RIGHT, "Select Morph", filterable: true, noDefaultSelection: true));
      SetMorphChooserChoices();
      Draw(VaMUI.CreateButton(VaMUI.RIGHT, "Add Morph", HandleAddMorph));
      foreach (TrackedMorph tm in activeLayer.trackedMorphs)
      {
        Draw(VaMUI.CreateLabelWithX(VaMUI.RIGHT, tm.standardName, () => { HandleDeleteMorph(tm); }));
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
      VaMUtils.SelectStringChooserFirstValue(activeLayerIdStorable);
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
      activeLayer.TrackMorph(morph);

      morphChooserStorable.valNoCallback = "";
      RequestRedraw();
    }

    void HandleDeleteMorph(TrackedMorph tm)
    {
      if (activeLayer == null) return;
      activeLayer.trackedMorphs.Remove(tm);
      RequestRedraw();
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
      CreateMainHeader(VaMUI.LEFT, "Animations");
      Draw(VaMUI.CreateInfoTextNoScroll(
        VaMUI.LEFT,
        @"An <b>Animation</b> defines how a <b>Layer</b> should evolve over time. An animation is composed of one or more <b>Keyframes</b> connected by tweens.",
        185f
      ));

      if (activeLayer == null)
      {
        Draw(VaMUI.CreateSpacer(VaMUI.RIGHT, 50f));
        Draw(VaMUI.CreateInfoTextNoScroll(
          VaMUI.RIGHT,
          @"You must select a <b>Layer</b> before you can create any <b>Animations</b>.",
          2
        ));
        return;
      }

      CreateBasicFunctionsUI(
        "Anim.",
        activeAnimation == null,
        () => { Draw(VaMUI.CreateOnelineTextInput(ref editAnimationNameStorable, VaMUI.RIGHT, "Name", activeAnimation?.name ?? "", callback: HandleRenameAnimation)); },
        HandleNewAnimation,
        HandleDuplicateAnimation,
        HandleDeleteAnimation
      );

      if (activeAnimation == null) return;

      CreateSubHeader(VaMUI.RIGHT, "Animation Details");
      Draw(VaMUI.CreateStringChooserFromStorable(activeAnimation.loopTypeStorable, VaMUI.RIGHT));
      Draw(VaMUI.CreateSliderFromStorable(activeAnimation.playbackSpeedStorable, VaMUI.RIGHT, 1f, 0f, 2f));

      CreateSubHeader(VaMUI.LEFT, "Keyframe Defaults");
      Draw(VaMUI.CreateSliderFromStorable(activeAnimation.defaultDurationStorable, VaMUI.LEFT, 1f, 0f, 10f));
      Draw(VaMUI.CreateStringChooserFromStorable(activeAnimation.defaultEasingStorable, VaMUI.LEFT));

      Draw(VaMUI.CreateSpacer(VaMUI.LEFT));
      CreateSubHeader(VaMUI.LEFT, "Actions");
      Draw(VaMUI.CreateButton(VaMUI.LEFT, "On Enter Animation", activeAnimation.onEnterTrigger.OpenPanel));
      Draw(VaMUI.CreateButton(VaMUI.LEFT, "On Animation Playing", activeAnimation.onPlayingTrigger.OpenPanel));
      Draw(VaMUI.CreateButton(VaMUI.LEFT, "On Exit Animation", activeAnimation.onExitTrigger.OpenPanel));


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
      VaMUtils.SelectStringChooserFirstValue(activeAnimationIdStorable);
    }


    // =============================================================================== //
    // ================================ KEYFRAMES TAB ================================ //
    // =============================================================================== //
    Animation.Keyframe activeKeyframe;

    void BuildKeyframesTabUI()
    {
      CreateMainHeader(VaMUI.LEFT, "Keyframes");
      Draw(VaMUI.CreateInfoTextNoScroll(
        VaMUI.LEFT,
        @"A <b>Keyframe</b> is a snapshot of a <b>Layer</b>'s state that records morph and controller values. An <b>Animation</b> is composed of one or more keyframes.",
        185f
      ));

      if (activeAnimation == null)
      {
        Draw(VaMUI.CreateSpacer(VaMUI.RIGHT, 50f));
        Draw(VaMUI.CreateInfoTextNoScroll(
          VaMUI.RIGHT,
          @"You must select an <b>Animation</b> before you can create any <b>Keyframes</b>.",
          2
        ));
        return;
      }

      EnsureSelectedKeyframe();

      // KEYFRAME SELECTOR
      CreateSubHeader(VaMUI.LEFT, "Keyframe Selector");
      if (activeAnimation.keyframes.Count == 0)
      {
        Draw(VaMUI.CreateButton(VaMUI.LEFT, "New Keyframe", HandleAddKeyframeStart));
        return;
      }
      Draw(VaMUI.CreateButtonPair(VaMUI.LEFT, "<< New Keyframe", HandleAddKeyframeStart, "New Keyframe >>", HandleAddKeyframeEnd));
      Draw(VaMUI.CreateButtonPair(VaMUI.LEFT, "< New Keyframe", HandleAddKeyframeBefore, "New Keyframe >", HandleAddKeyframeAfter));
      Draw(VaMUI.CreateButton(VaMUI.LEFT, "Duplicate Keyframe", HandleDuplicateKeyframe));
      Draw(VaMUI.CreateButtonPair(VaMUI.LEFT, "< Move Keyframe", () => { HandleMoveKeyframe(-1); }, "Move Keyframe >", () => { HandleMoveKeyframe(1); }));
      Draw(CreateKeyframeSelector(activeAnimation.keyframes, activeKeyframe, VaMUI.LEFT, HandleSelectKeyframe));

      if (activeKeyframe == null) return;

      Draw(VaMUI.CreateOnelineTextInputFromStorable(activeKeyframe.labelStorable, VaMUI.LEFT));
      activeKeyframe.labelStorable.setCallbackFunction = HandleSetKeyframeLabel;

      Draw(VaMUI.CreateColorPickerFromStorable(activeKeyframe.colorStorable, VaMUI.LEFT));
      Draw(VaMUI.CreateButton(VaMUI.LEFT, "Apply Color", () => { RequestRedraw(); }));

      // ACTIONS
      Draw(VaMUI.CreateSpacer(VaMUI.LEFT));
      CreateSubHeader(VaMUI.LEFT, "Actions");
      Draw(VaMUI.CreateButton(VaMUI.LEFT, "On Enter Keyframe", activeKeyframe.onEnterTrigger.OpenPanel));
      Draw(VaMUI.CreateButton(VaMUI.LEFT, "On Keyframe Playing", activeKeyframe.onPlayingTrigger.OpenPanel));
      Draw(VaMUI.CreateButton(VaMUI.LEFT, "On Exit Keyframe", activeKeyframe.onExitTrigger.OpenPanel));

      // KEYFRAME DETAILS
      CreateSubHeader(VaMUI.RIGHT, "Keyframe Details");
      Draw(VaMUI.CreateButton(VaMUI.RIGHT, "Capture Current State", HandleCaptureKeyframe, color: VaMUI.YELLOW));
      Draw(VaMUI.CreateButton(VaMUI.RIGHT, "Go To Keyframe", HandleGoToKeyframe));
      Draw(VaMUI.CreateSpacer(VaMUI.RIGHT));
      Draw(VaMUI.CreateButton(VaMUI.RIGHT, "Delete Keyframe", HandleDeleteKeyframe, color: VaMUI.RED));
      Draw(VaMUI.CreateSpacer(VaMUI.RIGHT));

      Draw(VaMUI.CreateSliderFromStorable(activeKeyframe.durationStorable, VaMUI.RIGHT, 1f, 0f, 10f));
      Draw(VaMUI.CreateStringChooserFromStorable(activeKeyframe.easingStorable, VaMUI.RIGHT));
      Draw(VaMUI.CreateSpacer(VaMUI.RIGHT));

      // MORPHS
      CreateSubHeader(VaMUI.RIGHT, "Morph Captures");
      foreach (TrackedMorph tm in activeKeyframe.animation.layer.trackedMorphs)
      {
        CapturedMorph capture = activeKeyframe.GetCapturedMorph(tm.morph.uid);
        tm.UpdateStorableToMorph();
        Draw(VaMUI.CreateSliderFromStorable(tm.storable, VaMUI.RIGHT, tm.defaultValue, tm.defaultMin, tm.defaultMax));
        string str = capture == null ? "<b>Val</b>: <NO DATA>" : $"<b>Val</b>: {capture.value}";
        Draw(VaMUI.CreateInfoTextNoScroll(VaMUI.RIGHT, $"<size=24>{str}</size>", 1, background: false));
      }
      if (activeKeyframe.animation.layer.trackedMorphs.Count == 0)
      {
        Draw(VaMUI.CreateInfoTextNoScroll(VaMUI.RIGHT, "<none>", 1, background: false));
      }
      Draw(VaMUI.CreateSpacer(VaMUI.RIGHT));

      // CONTROLLERS
      CreateSubHeader(VaMUI.RIGHT, "Controller Captures");
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
        Draw(VaMUI.CreateInfoTextNoScroll(VaMUI.RIGHT, str, 2, background: false));
      }
      if (controllerCount == 0)
      {
        Draw(VaMUI.CreateInfoTextNoScroll(VaMUI.RIGHT, "<none>", 1, background: false));
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
      RequestRedraw();
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
      RequestRedraw();
    }

    void HandleAddKeyframeEnd()
    {
      if (activeAnimation == null) return;
      Animation.Keyframe keyframe = new Animation.Keyframe(activeAnimation, -1);
      HandleSelectKeyframe(keyframe);
      RequestRedraw();
    }

    void HandleAddKeyframeBefore()
    {
      int? index = GetActiveKeyframeIndex();
      if (activeAnimation == null || index == null) return;
      Animation.Keyframe keyframe = new Animation.Keyframe(activeAnimation, index.Value);
      HandleSelectKeyframe(keyframe);
      RequestRedraw();
    }

    void HandleAddKeyframeAfter()
    {
      int? index = GetActiveKeyframeIndex();
      if (activeAnimation == null || index == null) return;
      Animation.Keyframe keyframe = new Animation.Keyframe(activeAnimation, index.Value + 1);
      HandleSelectKeyframe(keyframe);
      RequestRedraw();
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
      RequestRedraw();
    }

    void HandleDuplicateKeyframe()
    {
      int? index = GetActiveKeyframeIndex();
      if (activeKeyframe == null || index == null) return;
      Animation.Keyframe keyframe = activeKeyframe.Clone(index: index.Value + 1);
      HandleSelectKeyframe(keyframe);
      RequestRedraw();
    }

    void HandleDeleteKeyframe()
    {
      if (activeKeyframe == null) return;
      activeKeyframe.Delete();
      RequestRedraw();
    }

    void HandleSetKeyframeLabel(string val)
    {
      RequestRedraw();
    }

    void HandleCaptureKeyframe()
    {
      if (activeKeyframe == null) return;
      activeKeyframe.CaptureLayerState();
      RequestRedraw();
    }

    void HandleGoToKeyframe()
    {
      if (activeKeyframe == null) return;

      Transform mainTransform = person.mainController.transform;
      foreach (TrackedController tc in activeKeyframe.animation.layer.trackedControllers)
      {
        if (!tc.isTracked) continue;
        Transform controllerTransform = tc.controller.transform;
        CapturedController capture = activeKeyframe.GetCapturedController(tc.controller.name);
        if (tc.trackPositionStorable.val && capture?.position != null)
        {
          controllerTransform.position = mainTransform.TransformPoint(capture.position.Value);
        }
        if (tc.trackRotationStorable.val && capture?.rotation != null)
        {
          controllerTransform.rotation = mainTransform.rotation * capture.rotation.Value;
        }
      }

      foreach (TrackedMorph tm in activeKeyframe.animation.layer.trackedMorphs)
      {
        CapturedMorph capture = activeKeyframe.GetCapturedMorph(tm.morph.uid);
        if (capture != null)
        {
          tm.morph.SetValue(capture.value);
          tm.UpdateStorableToMorph();
        }
      }
    }


    // ================================================================================= //
    // ================================ TRANSITIONS TAB ================================ //
    // ================================================================================= //
    void BuildTransitionsTabUI()
    {
      CreateMainHeader(VaMUI.LEFT, "Transitions");
      Draw(VaMUI.CreateSpacer(VaMUI.RIGHT, 45f));

      Draw(VaMUI.CreateInfoTextNoScroll(
        VaMUI.LEFT,
        @"A <b>Transition</b> defines how to move from one animation to another. The default transition is a simple tween, but if more control is needed <b>Keyframes</b> may be added for precision.",
        5
      ));


      CreateBottomPadding();
    }


    // =================================================================================== //
    // ================================ SEND MESSAGES TAB ================================ //
    // =================================================================================== //
    void BuildSendMessagesTabUI()
    {
      CreateMainHeader(VaMUI.LEFT, "Send Messages");
      Draw(VaMUI.CreateSpacer(VaMUI.RIGHT, 45f));

      Draw(VaMUI.CreateInfoTextNoScroll(
        VaMUI.LEFT,
        @"A <b>Message</b> is a custom event that can be called from other atoms in your scene using the 'Send Message' action. Messages allow external management of the character's state.",
        5
      ));


      CreateBottomPadding();
    }


    // ====================================================================================== //
    // ================================ RECEIVE MESSAGES TAB ================================ //
    // ====================================================================================== //
    void BuildReceiveMessagesTabUI()
    {
      CreateMainHeader(VaMUI.LEFT, "Receive Messages");
      Draw(VaMUI.CreateSpacer(VaMUI.RIGHT, 45f));

      Draw(VaMUI.CreateInfoTextNoScroll(
        VaMUI.LEFT,
        @"A <b>Message</b> is a custom event that can be called from other atoms in your scene using the 'Send Message' action. Messages allow external management of the character's state.",
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
    public UIDynamicControllerSelector CreateControllerSelector(TrackedController tc, VaMUI.Column side)
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

        UIDynamicControllerSelector uid = VaMUI.CreateUIDynamicPrefab<UIDynamicControllerSelector>("LabelWithToggle", 50f);
        controllerSelectorPrefab = uid.gameObject;

        RectTransform background = VaMUI.InstantiateBackground(uid.transform);
        Image bgImage = background.GetComponent<Image>();
        bgImage.color = new Color(1f, 1f, 1f, 0.35f);

        RectTransform labelRect = VaMUI.InstantiateLabel(uid.transform);
        labelRect.offsetMin = new Vector2(5f, 0f);
        Text labelText = labelRect.GetComponent<Text>();
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.text = "";

        // == POS == //
        RectTransform posBackgroundRect = VaMUI.InstantiateBackground(uid.transform);
        posBackgroundRect.anchorMin = new Vector2(1f, 0f);
        posBackgroundRect.anchorMax = new Vector2(1f, 1f);
        posBackgroundRect.offsetMin = new Vector2(-(2f * bgSize + bgSpacing), 0f);
        posBackgroundRect.offsetMax = new Vector2(-(bgSize + bgSpacing), 0f);
        Image posBackgroundImg = posBackgroundRect.GetComponent<Image>();
        posBackgroundImg.color = new Color(1f, 1f, 1f, 1f);

        RectTransform posToggleRect = VaMUI.InstantiateToggle(posBackgroundRect, checkSize);
        posToggleRect.anchorMin = new Vector2(1f, 0f);
        posToggleRect.anchorMax = new Vector2(1f, 1f);
        posToggleRect.offsetMin = new Vector2(-checkSize - checkOffsetX, checkOffsetY);
        posToggleRect.offsetMax = new Vector2(-checkOffsetX, checkOffsetY);

        RectTransform posLabelRect = VaMUI.InstantiateLabel(posBackgroundRect);
        posLabelRect.offsetMin = new Vector2(labelOffsetX, labelOffsetY);
        posLabelRect.offsetMax = new Vector2(labelOffsetX, labelOffsetY);
        Text posLabel = posLabelRect.GetComponent<Text>();
        posLabel.alignment = TextAnchor.MiddleLeft;
        posLabel.text = "<size=20><b>POS</b></size>";

        // == ROT == //
        RectTransform rotBackgroundRect = VaMUI.InstantiateBackground(uid.transform);
        rotBackgroundRect.anchorMin = new Vector2(1f, 0f);
        rotBackgroundRect.anchorMax = new Vector2(1f, 1f);
        rotBackgroundRect.offsetMin = new Vector2(-bgSize, 0f);
        rotBackgroundRect.offsetMax = new Vector2(0f, 0f);
        Image rotBackgroundImg = rotBackgroundRect.GetComponent<Image>();
        rotBackgroundImg.color = new Color(1f, 1f, 1f, 1f);

        RectTransform rotToggleRect = VaMUI.InstantiateToggle(rotBackgroundRect, checkSize);
        rotToggleRect.anchorMin = new Vector2(1f, 0f);
        rotToggleRect.anchorMax = new Vector2(1f, 1f);
        rotToggleRect.offsetMin = new Vector2(-checkSize - checkOffsetX, checkOffsetY);
        rotToggleRect.offsetMax = new Vector2(-checkOffsetX, checkOffsetY);

        RectTransform rotLabelRect = VaMUI.InstantiateLabel(rotBackgroundRect);
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
        Transform t = CreateUIElement(controllerSelectorPrefab.transform, side == VaMUI.RIGHT);
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

      private const int buttonsPerRow = 4;
      private const float buttonWidthScale = 1f / (float)buttonsPerRow;
      private const float buttonHeight = 70f;
      private const float buttonSpacing = 10f;

      void Start()
      {
        int rows = (keyframes.Count - 1) / buttonsPerRow + 1;
        float totalHeight = rows * (buttonHeight + buttonSpacing) - buttonSpacing;
        layout.preferredHeight = layout.minHeight = totalHeight + 10f;

        for (int i = 0; i < keyframes.Count; i++)
        {
          Animation.Keyframe keyframe = keyframes[i];
          int x = i % buttonsPerRow;
          int y = i / buttonsPerRow;
          float yOffset = (buttonHeight + buttonSpacing) * y;

          RectTransform buttonRect = VaMUI.InstantiateButton(buttonContainer);
          buttonRect.anchorMin = new Vector2(buttonWidthScale * x, 1f);
          buttonRect.anchorMax = new Vector2(buttonWidthScale * x + buttonWidthScale, 1f);
          buttonRect.offsetMin = new Vector2(buttonSpacing / 2f, -buttonHeight - yOffset);
          buttonRect.offsetMax = new Vector2(-buttonSpacing / 2f, 0f - yOffset);

          UIDynamicButton button = buttonRect.GetComponent<UIDynamicButton>();
          button.buttonText.rectTransform.anchorMin = new Vector2(0f, 0.4f);
          button.buttonText.rectTransform.offsetMin = new Vector2(5f, 0f);
          button.buttonText.text = $"<size=25>{i + 1}</size>";
          if (keyframe.id == activeKeyframe?.id)
          {
            button.buttonText.text = $"<size=25>[ {i + 1} ]</size>";
          }
          button.buttonText.alignment = TextAnchor.UpperLeft;
          HSVColor color = keyframe.colorStorable.val;
          button.buttonColor = Color.HSVToRGB(color.H, color.S, color.V);

          RectTransform labelRect = VaMUI.InstantiateLabel(buttonRect);
          labelRect.anchorMax = new Vector2(1f, 0.6f);
          labelRect.offsetMin = new Vector2(5f, 5f);
          Text labelText = labelRect.GetComponent<Text>();
          labelText.text = $"<size=22>{keyframe.labelStorable.val}</size>";
          labelText.alignment = TextAnchor.LowerLeft;

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
    public UIDynamicKeyframeSelector CreateKeyframeSelector(List<Animation.Keyframe> keyframes, Animation.Keyframe activeKeyframe, VaMUI.Column side, SelectKeyframeCallback callback = null)
    {
      if (keyframeSelectorPrefab == null)
      {
        UIDynamicKeyframeSelector uid = VaMUI.CreateUIDynamicPrefab<UIDynamicKeyframeSelector>("KeyframeSelector", 0f);
        keyframeSelectorPrefab = uid.gameObject;
        RectTransform background = VaMUI.InstantiateBackground(uid.transform);
        RectTransform buttonContainer = VaMUI.InstantiateEmptyRect(uid.transform);
        buttonContainer.offsetMin = new Vector2(0f, 5f);
        buttonContainer.offsetMax = new Vector2(0f, -5f);
        uid.buttonContainer = buttonContainer;
      }
      {
        Transform t = CreateUIElement(keyframeSelectorPrefab.transform, side == VaMUI.RIGHT);
        UIDynamicKeyframeSelector uid = t.GetComponent<UIDynamicKeyframeSelector>();
        uid.activeKeyframe = activeKeyframe;
        uid.keyframes = keyframes;
        uid.callback = callback;
        uid.gameObject.SetActive(true);
        return uid;
      }
    }


    // ====== PlaylistEntry Container ====== //
    public class UIDynamicPlaylistEntryContainer : UIDynamicBase
    {
      public string playlistMode;
      public string timingMode;
      public RectTransform background;
      public Text indexText;
    }
    
    private GameObject playlistEntryContainerPrefab;
    public UIDynamicPlaylistEntryContainer CreatePlaylistEntryContainer(string playlistMode, string timingMode, int index, VaMUI.Column side)
    {
      if (playlistEntryContainerPrefab == null)
      {
        UIDynamicPlaylistEntryContainer uid = VaMUI.CreateUIDynamicPrefab<UIDynamicPlaylistEntryContainer>("PlaylistEntryContainer", 0f);
        playlistEntryContainerPrefab = uid.gameObject;

        RectTransform background = VaMUI.InstantiateBackground(uid.transform);
        Image bgImage = background.GetComponent<Image>();
        bgImage.color = new Color(0.9f, 0.9f, 0.9f, 0.5f);

        RectTransform textRect = VaMUI.InstantiateLabel(uid.transform);
        textRect.offsetMin = new Vector2(8f, -50f);
        textRect.offsetMax = new Vector2(0f, -8f);
        Text text = textRect.GetComponent<Text>();
        text.alignment = TextAnchor.UpperLeft;
        text.fontSize = 24;

        uid.background = background;
        uid.indexText = text;
      }
      {
        const float sliderHeight = 136f;
        Transform t = CreateUIElement(playlistEntryContainerPrefab.transform, side == VaMUI.RIGHT);
        UIDynamicPlaylistEntryContainer uid = t.GetComponent<UIDynamicPlaylistEntryContainer>();
        uid.indexText.text = index.ToString();
        float height = 280f;
        if (playlistMode == PlaylistMode.Random) height += sliderHeight;
        if (timingMode == TimingMode.FixedDuration) height += sliderHeight;
        if (timingMode == TimingMode.RandomDuration) height += 2f * sliderHeight;
        uid.background.offsetMin = new Vector2(0f, -height);
        uid.gameObject.SetActive(true);
        return uid;
      }
    }
  }
}
