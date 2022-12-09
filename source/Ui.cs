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
      public const string Roles = "Roles";
      public const string Messages = "Messages";
      public const string ExportImport = "Export/Import";

      public static readonly string[] list = new string[] { Info, Groups, States, Layers, Animations, Keyframes, Roles, Messages, ExportImport };
    }

    bool uiNeedsRebuilt = false;
    List<object> uiItems = new List<object>();

    const float INFO_REFRESH_TIME = 0.1f;
    float infoRefreshTimer = 0f;

    GameObject tabBarPrefab;
    string activeTab = Tabs.Info;

    // Global UI
    VaMUI.VaMToggle playbackEnabledToggle;
    VaMUI.VaMToggle hideTopUIToggle;

    Group activeGroup = null;
    State activeState = null;
    Layer activeLayer = null;
    Animation activeAnimation = null;

    VaMUI.VaMStringChooser activeGroupIdChooser;
    VaMUI.VaMStringChooser activeStateIdChooser;
    VaMUI.VaMStringChooser activeLayerIdChooser;
    VaMUI.VaMStringChooser activeAnimationIdChooser;

    VaMUI.VaMTextInput editGroupNameInput;
    VaMUI.VaMTextInput editStateNameInput;
    VaMUI.VaMTextInput editLayerNameInput;
    VaMUI.VaMTextInput editAnimationNameInput;

    // Section UI
    Dictionary<Group, UIDynamicInfoText> infoTexts = new Dictionary<Group, UIDynamicInfoText>();

    AnimationPlaylist activePlaylist;
    Animation.Keyframe activeKeyframe;
    AnimationPlayer previewAnimationPlayer = null;

    VaMUI.VaMStringChooser transitionStateChooser;
    VaMUI.VaMStringChooser addMorphChooser;
    VaMUI.VaMToggle morphChooserUseFavoritesToggle;

    void UIInit()
    {
      VaMTrigger.Init(this);
      VaMUI.Init(this, CreateUIElement);

      playbackEnabledToggle = VaMUI.CreateToggle("Playback Enabled", true, register: true, callbackNoVal: DestroyPreviewPlayer);
      hideTopUIToggle = VaMUI.CreateToggle("Hide Top UI", false, callbackNoVal: RequestRedraw);

      activeGroupIdChooser = VaMUI.CreateStringChooserKeyVal("Group", callbackNoVal: HandleSelectGroup);
      activeStateIdChooser = VaMUI.CreateStringChooserKeyVal("State", callbackNoVal: HandleSelectState);
      activeLayerIdChooser = VaMUI.CreateStringChooserKeyVal("Layer", callbackNoVal: HandleSelectLayer);
      activeAnimationIdChooser = VaMUI.CreateStringChooserKeyVal("Animation", callbackNoVal: HandleSelectAnimation);

      editGroupNameInput = VaMUI.CreateTextInput("Name", callback: HandleRenameGroup);
      editStateNameInput = VaMUI.CreateTextInput("Name", callback: HandleRenameState);
      editLayerNameInput = VaMUI.CreateTextInput("Name", callback: HandleRenameLayer);
      editAnimationNameInput = VaMUI.CreateTextInput("Name", callback: HandleRenameAnimation);

      transitionStateChooser = VaMUI.CreateStringChooserKeyVal("Select State", null, "");
      addMorphChooser = VaMUI.CreateStringChooserKeyVal("Select Morph", filterable: true, defaultValue: "");
      morphChooserUseFavoritesToggle = VaMUI.CreateToggle("Favorites Only", true, callbackNoVal: HandleToggleMorphChooserFavorites);

      Role.Init();
      Messages.Init();

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
        RebuildUI();
        uiNeedsRebuilt = false;
      }
      RefreshInfoText();
      UpdatePreviewAnimationPlayer();
    }

    void RefreshUIAfterJSONLoad()
    {
      RefreshGroupList();
      RefreshLayerList();
      VaMUtils.SelectStringChooserFirstValue(activeGroupIdChooser.storable);
      VaMUtils.SelectStringChooserFirstValue(activeLayerIdChooser.storable);
    }

    public void RequestRedraw()
    {
      uiNeedsRebuilt = true;
      infoRefreshTimer = 1f;
    }

    void UI(object item)
    {
      uiItems.Add(item);
    }

    void RebuildUI()
    {
      VaMUI.RemoveUIElements(ref uiItems);
      infoTexts.Clear();

      UI(playbackEnabledToggle.Draw(VaMUI.LEFT));
      UI(hideTopUIToggle.Draw(VaMUI.RIGHT));

      if (!hideTopUIToggle.val)
      {
        UI(activeGroupIdChooser.Draw(VaMUI.LEFT));
        UI(activeStateIdChooser.Draw(VaMUI.RIGHT));
        UI(activeLayerIdChooser.Draw(VaMUI.LEFT));
        UI(activeAnimationIdChooser.Draw(VaMUI.RIGHT));

        UI(VaMUI.CreateTabBar(ref tabBarPrefab, VaMUI.LEFT, Tabs.list, HandleTabSelect, 5));
        UI(VaMUI.CreateSpacer(VaMUI.RIGHT, 2 * 55f));
      }

      BuildActiveTabUI();
      CreateBottomPadding();
    }

    void HandleTabSelect(string tabName)
    {
      activeTab = tabName;
      if (activeTab != Tabs.Keyframes)
      {
        DestroyPreviewPlayer();
      } 
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
        case Tabs.Roles:
          BuildRolesTabUI();
          break;
        case Tabs.Messages:
          BuildMessagesTabUI();
          break;
        case Tabs.ExportImport:
          BuildExportImportTabUI();
          break;
        default:
          break;
      }
    }

    void HandleSelectGroup()
    {
      VaMUtils.EnsureStringChooserValue(activeGroupIdChooser.storable, defaultToFirstChoice: true, noCallback: true);
      activeGroup = Group.list.Find((g) => g.id == activeGroupIdChooser.val);

      RefreshStateList();
      VaMUtils.SelectStringChooserFirstValue(activeStateIdChooser.storable);

      editGroupNameInput.valNoCallback = activeGroup?.name ?? "";

      RequestRedraw();
    }

    void HandleSelectState()
    {
      VaMUtils.EnsureStringChooserValue(activeStateIdChooser.storable, defaultToFirstChoice: true, noCallback: true);
      activeState = activeGroup?.states.Find((s) => s.id == activeStateIdChooser.val);

      editStateNameInput.valNoCallback = activeState?.name ?? "";

      RequestRedraw();
    }

    void HandleSelectLayer()
    {
      VaMUtils.EnsureStringChooserValue(activeLayerIdChooser.storable, defaultToFirstChoice: true, noCallback: true);
      activeLayer = Layer.list.Find((l) => l.id == activeLayerIdChooser.val);

      RefreshAnimationList();
      VaMUtils.SelectStringChooserFirstValue(activeAnimationIdChooser.storable);

      editLayerNameInput.valNoCallback = activeLayer?.name ?? "";

      RequestRedraw();
    }

    void HandleSelectAnimation()
    {
      VaMUtils.EnsureStringChooserValue(activeAnimationIdChooser.storable, defaultToFirstChoice: true, noCallback: true);
      activeAnimation = activeLayer?.animations.Find((a) => a.id == activeAnimationIdChooser.val);

      editAnimationNameInput.valNoCallback = activeAnimation?.name ?? "";

      DestroyPreviewPlayer();

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
      VaMUtils.SetStringChooserChoices(activeGroupIdChooser.storable, entries);

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
      VaMUtils.SetStringChooserChoices(activeStateIdChooser.storable, entries);

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
      VaMUtils.SetStringChooserChoices(activeLayerIdChooser.storable, entries);

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
      VaMUtils.SetStringChooserChoices(activeAnimationIdChooser.storable, entries);

      RequestRedraw();
    }

    void CreateMainHeader(VaMUI.Column side, string text)
    {
      UI(VaMUI.CreateHeaderText(side, text, 50f));
    }

    void CreateSubHeader(VaMUI.Column side, string text)
    {
      UI(VaMUI.CreateHeaderText(side, text, 38f));
    }

    void CreateBasicFunctionsUI(string category, bool showNewOnly, VaMUI.VaMTextInput nameInput, UnityAction newHandler, UnityAction duplicateHandler, UnityAction deleteHandler)
    {
      if (showNewOnly)
      {
        UI(VaMUI.CreateSpacer(VaMUI.RIGHT, 50f));
        UI(VaMUI.CreateButton(VaMUI.RIGHT, $"New {category}", newHandler, color: VaMUI.GREEN));
      }
      else
      {
        UI(nameInput.Draw(VaMUI.RIGHT));
        UI(VaMUI.CreateButtonPair(VaMUI.RIGHT, $"New {category}", newHandler, $"Duplicate {category}", duplicateHandler, VaMUI.GREEN, VaMUI.BLUE));
        UI(VaMUI.CreateSpacer(VaMUI.RIGHT));
        UI(VaMUI.CreateButton(VaMUI.RIGHT, $"Delete {category}", deleteHandler, color: VaMUI.RED));
        UI(VaMUI.CreateSpacer(VaMUI.RIGHT));
      }
    }

    void CreateBottomPadding()
    {
      UI(VaMUI.CreateSpacer(VaMUI.LEFT, 100f));
      UI(VaMUI.CreateSpacer(VaMUI.RIGHT, 100f));
    }


    // ========================================================================== //
    // ================================ INFO TAB ================================ //
    // ========================================================================== //
    void BuildInfoTabUI()
    {
      // CreateMainHeader(VaMUI.LEFT, "Info");
      CreateMainHeader(VaMUI.RIGHT, "Currently Playing");

      UI(VaMUI.CreateInfoText(
        VaMUI.LEFT,
        "Quickstart guide:\n\n"
        + "- Create a <b>Group</b>, a <b>State</b>, a <b>Layer</b>, and an <b>Animation</b>\n\n"
        + "- In the <b>Layers</b> tab, select the controllers and morphs you want to capture\n\n"
        + "- In the <b>Keyframes</b> tab, add a keyframe, pose your model, and click <b>Capture Current State</b>\n\n"
        + "- In the <b>States</b> tab, click <b>Add Current Layer</b> and then click <b>Add Current Animation</b>\n\n"
        + "- In the <b>Groups</b> tab, click <b>Set As Initial State</b>\n\n"
        + "- Congratulations! You now have a basic animation with a single keyframe.",
        23
      ));

      UI(VaMUI.CreateInfoText(
        VaMUI.LEFT,
        "<b>CharacterStateManager</b> is meant to be a powerful tool for layering multiple state machines to control a character in complex ways. The main goal was to create an all-in-one plugin that could create dynamic procedural behavior. It has many options and there are many ways to do things, so have fun exploring and thinking creatively.",
        10
      ));

      if (GroupPlayer.list.Count == 0)
      {
        UI(VaMUI.CreateInfoText(
          VaMUI.RIGHT,
          "Nothing is currently playing",
          1
        ));
      }
      else
      {
        foreach (KeyValuePair<Group, GroupPlayer> entry in GroupPlayer.list)
        {
          CreateSubHeader(VaMUI.RIGHT, entry.Key.name);
          infoTexts[entry.Key] = VaMUI.CreateInfoText(VaMUI.RIGHT, "", 0f);
          UI(infoTexts[entry.Key]);
        }
      }
    }

    void RefreshInfoText()
    {
      if (activeTab == Tabs.Info && infoTexts != null)
      {
        infoRefreshTimer += Time.deltaTime;
        if (infoRefreshTimer < INFO_REFRESH_TIME) return;
        infoRefreshTimer = 0f;
        foreach (KeyValuePair<Group, GroupPlayer> entry in GroupPlayer.list)
        {
          if (!infoTexts.ContainsKey(entry.Key)) continue;
          UIDynamicInfoText infoText = infoTexts[entry.Key];

          Group group = entry.Key;
          GroupPlayer groupPlayer = entry.Value;
          StatePlayer statePlayer = groupPlayer.statePlayer;

          string str = "";
          int lines = 0;

          str += $"State: <b>{statePlayer.currentState?.name ?? "<none>"}</b>";
          str += $"\nTime: {statePlayer.time:F1}s";
          lines += 2;

          foreach (KeyValuePair<AnimationPlaylist, PlaylistPlayer> ppEntry in statePlayer.playlistPlayers)
          {
            AnimationPlaylist playlist = ppEntry.Key;
            PlaylistPlayer playlistPlayer = ppEntry.Value;
            PlaylistEntryPlayer playlistEntryPlayer = playlistPlayer.playlistEntryPlayer;
            AnimationPlayer animationPlayer = playlistEntryPlayer.animationPlayer;
            KeyframePlayer keyframePlayer = animationPlayer.keyframePlayer;
            int currentKeyframeIndex = animationPlayer.GetKeyframeIndex(keyframePlayer.currentKeyframe);
            int targetKeyframeIndex = animationPlayer.GetKeyframeIndex(keyframePlayer.targetKeyframe);
            string playlistTimeStr = $"{playlistEntryPlayer.time:F1}s / {playlistEntryPlayer.targetTime:F1}s";
            string animationTimeStr = $"{animationPlayer.time:F1}s ({animationPlayer.progress:F1})";
            string currentKeyframeStr = currentKeyframeIndex == -1 ? "?" : $"{currentKeyframeIndex + 1}";
            string targetKeyframeStr = targetKeyframeIndex == -1 ? "?" : $"{targetKeyframeIndex + 1}";

            str += $"\n\n<b>{playlist.layer.name}</b>";
            str += $"\n------";
            str += $"\nAnimation: <b>{animationPlayer.currentAnimation?.name ?? "<none>"}</b>";
            str += $"\nPlaylist Time: {(keyframePlayer.playingInBetweenKeyframe ? "<transitioning>" : playlistTimeStr)}";
            str += $"\nTime: {(keyframePlayer.playingInBetweenKeyframe ? "<transitioning>" : animationTimeStr)}";
            str += $"\nKeyframe: <b>{currentKeyframeStr}</b> -> <b>{targetKeyframeStr}</b>";
            str += $"\nKeyframe Time: {keyframePlayer.time:F1}s ({keyframePlayer.progress:F1})";
            lines += 8;
          }

          infoText.text.text = str;
          infoText.height = lines * 32f + 8f;
        }
      }
    }


    // ============================================================================ //
    // ================================ GROUPS TAB ================================ //
    // ============================================================================ //
    void BuildGroupsTabUI()
    {
      CreateMainHeader(VaMUI.LEFT, "Groups");
      UI(VaMUI.CreateInfoText(
        VaMUI.LEFT,
        "A <b>Group</b> is a collection of <b>States</b> that are independent from other groups -- basically a self-contained state machine. Use multiple groups to layer multiple behaviors together.",
        185f
      ));

      CreateBasicFunctionsUI(
        "Group",
        activeGroup == null,
        editGroupNameInput,
        HandleNewGroup,
        HandleDuplicateGroup,
        HandleDeleteGroup
      );

      if (activeGroup == null) return;

      if (activeState == null)
      {
        UI(VaMUI.CreateSpacer(VaMUI.LEFT, 50f));
        UI(VaMUI.CreateInfoText(
          VaMUI.LEFT,
          "You must add at least one <b>State</b> to configure this group.",
          2
        ));
        return;
      }

      CreateSubHeader(VaMUI.LEFT, "Group Settings");
      UI(VaMUI.CreateButton(VaMUI.LEFT, "Play Initial State", HandlePlayInitialState, VaMUI.YELLOW));
      UI(VaMUI.CreateButton(VaMUI.LEFT, "Play Selected State", HandlePlayCurrentState, VaMUI.YELLOW));
      UI(activeGroup.playbackEnabledToggle.Draw(VaMUI.LEFT));
      UI(VaMUI.CreateButton(VaMUI.LEFT, "Set As Initial State", SetInitialState));
      UI(VaMUI.CreateButton(VaMUI.LEFT, "Unset Initial State", RemoveInitialState));
      UI(VaMUI.CreateInfoText(VaMUI.LEFT, $"<b>Initial State</b>: {activeGroup.initialState?.name ?? "<none>"}", 1, background: false));

      UI(VaMUI.CreateSpacer(VaMUI.LEFT));
      CreateSubHeader(VaMUI.LEFT, "State Settings");
      UI(activeState.transitionModeChooser.Draw(VaMUI.LEFT));
      if (activeState.transitionModeChooser.val == TransitionMode.None)
      {
        UI(VaMUI.CreateInfoText(VaMUI.LEFT, "The state will not automatically advance.", 2));
      }
      else if (activeState.transitionModeChooser.val == TransitionMode.PlaylistCompleted)
      {
        UI(VaMUI.CreateInfoText(VaMUI.LEFT, "The state will advance when any layer's playlist completes.", 2));
      }
      else if (activeState.transitionModeChooser.val == TransitionMode.FixedDuration)
      {
        UI(VaMUI.CreateInfoText(VaMUI.LEFT, "The state will advance after a fixed duration.", 2));
        UI(activeState.fixedDurationSlider.Draw(VaMUI.LEFT));
      }
      else if (activeState.transitionModeChooser.val == TransitionMode.RandomDuration)
      {
        UI(VaMUI.CreateInfoText(VaMUI.LEFT, "The state will advance after a random duration.", 2));
        UI(activeState.minDurationSlider.Draw(VaMUI.LEFT));
        UI(activeState.maxDurationSlider.Draw(VaMUI.LEFT));
      }
      UI(VaMUI.CreateSpacer(VaMUI.LEFT, 100f));

      CreateSubHeader(VaMUI.RIGHT, "State Transitions");
      UI(VaMUI.CreateInfoText(VaMUI.RIGHT, "The current state can transition to states added here.", 2));
      UI(transitionStateChooser.Draw(VaMUI.RIGHT));
      SetTransitionStateChooserChoices();
      UI(VaMUI.CreateButton(VaMUI.RIGHT, "Add State Transition", HandleAddStateTransition));
      UI(VaMUI.CreateSpacer(VaMUI.RIGHT));

      foreach (StateTransition transition in activeState.transitions)
      {
        UI(VaMUI.CreateLabelWithX(VaMUI.RIGHT, transition.state.name, () => { HandleRemoveStateTransition(transition); }));
        UI(transition.weightSlider.Draw(VaMUI.RIGHT));
        UI(VaMUI.CreateSpacer(VaMUI.RIGHT));
      }
    }

    void HandleNewGroup()
    {
      Group group = new Group();
      RefreshGroupList();
      activeGroupIdChooser.val = group.id;
    }

    void HandleDuplicateGroup()
    {
      if (activeGroup == null) return;
      Group group = activeGroup.Clone();
      RefreshGroupList();
      activeGroupIdChooser.val = group.id;
    }

    void HandleRenameGroup(string val)
    {
      if (activeGroup == null) return;
      activeGroup.name = val;
      RefreshGroupList();
      activeGroupIdChooser.val = activeGroup.id;
    }

    void HandleDeleteGroup()
    {
      if (activeGroup == null) return;
      activeGroup.Dispose();
      RefreshGroupList();
      VaMUtils.SelectStringChooserFirstValue(activeGroupIdChooser.storable);
    }

    void HandlePlayInitialState()
    {
      if (activeGroup?.initialState == null) return;
      GroupPlayer.PlayState(activeGroup, activeGroup.initialState);
    }

    void HandlePlayCurrentState()
    {
      if (activeGroup == null || activeState == null) return;
      GroupPlayer.PlayState(activeGroup, activeState);
    }


    void SetInitialState()
    {
      if (activeGroup == null || activeState == null) return;
      activeGroup.initialState = activeState;
      RequestRedraw();
    }

    void RemoveInitialState()
    {
      if (activeGroup == null) return;
      activeGroup.initialState = null;
      RequestRedraw();
    }

    void SetTransitionStateChooserChoices()
    {
      transitionStateChooser.choices = activeStateIdChooser.choices;
      transitionStateChooser.displayChoices = activeStateIdChooser.displayChoices;
    }

    void HandleAddStateTransition()
    {
      if (activeGroup == null || activeState == null) return;
      State target = activeGroup.states.Find((s) => s.id == transitionStateChooser.val);
      if (target == null) return;
      activeState.AddTransition(target);
      transitionStateChooser.val = "";
      RequestRedraw();
    }

    void HandleRemoveStateTransition(StateTransition transition)
    {
      if (activeState == null) return;
      activeState.transitions.Remove(transition);
      RequestRedraw();
    }


    // ============================================================================ //
    // ================================ STATES TAB ================================ //
    // ============================================================================ //
    void BuildStatesTabUI()
    {
      SetActivePlaylist();

      CreateMainHeader(VaMUI.LEFT, "States");
      UI(VaMUI.CreateInfoText(
        VaMUI.LEFT,
        "A <b>State</b> defines what a character is currently doing (idle, sitting, etc). A state assigns <b>Animations</b> to <b>Layers</b> that can be played either sequentially or randomly.",
        185f
      ));

      if (activeGroup == null)
      {
        UI(VaMUI.CreateSpacer(VaMUI.RIGHT, 50f));
        UI(VaMUI.CreateInfoText(
          VaMUI.RIGHT,
          "You must select a <b>Group</b> before you can create any <b>States</b>.",
          2
        ));
        return;
      }

      CreateBasicFunctionsUI(
        "State",
        activeState == null,
        editStateNameInput,
        HandleNewState,
        HandleDuplicateState,
        HandleDeleteState
      );

      if (activeState == null) return;

      CreateSubHeader(VaMUI.LEFT, "Target Layers");
      UI(VaMUI.CreateInfoText(
        VaMUI.LEFT,
        "You can only assign animations to layers in this list.",
        2
      ));

      if (activeLayer == null)
      {
        UI(VaMUI.CreateInfoText(
          VaMUI.LEFT,
          "You have not created any <b>Layers</b>.",
          1
        ));

        DrawStateActions();
        return;
      }

      // LAYERS
      UI(VaMUI.CreateButton(VaMUI.LEFT, "Add Current Layer", HandleCreatePlaylist));
      foreach (AnimationPlaylist playlist in activeState.playlists)
      {
        bool isActive = playlist.layer == activeLayer;
        string label = isActive ? $"[ {playlist.layer.name} ]" : playlist.layer.name;
        UI(VaMUI.CreateLabelWithX(VaMUI.LEFT, label, () => { HandleDeletePlaylist(playlist); }));
      }

      CreateSubHeader(VaMUI.RIGHT, "Animation Playlist");
      if (activePlaylist == null)
      {
        UI(VaMUI.CreateInfoText(
          VaMUI.RIGHT,
          "Select or add a <b>Layer</b> to the list to edit its animation playlist.",
          2
        ));
        DrawStateActions();
        return;
      }

      if (activeAnimation == null)
      {
        UI(VaMUI.CreateInfoText(
          VaMUI.RIGHT,
          "There are no <b>Animations</b> available for this <b>Layer</b>.",
          2
        ));
        DrawStateActions();
        return;
      }

      // OPTIONS
      UI(VaMUI.CreateSpacer(VaMUI.LEFT));
      CreateSubHeader(VaMUI.LEFT, "Playlist Options");
      UI(activePlaylist.playModeChooser.Draw(VaMUI.LEFT));
      if (activePlaylist.playModeChooser.val == PlaylistMode.Sequential)
      {
        UI(VaMUI.CreateInfoText(
          VaMUI.LEFT,
          "The animations will play in order.",
          1
        ));
      }
      else if (activePlaylist.playModeChooser.val == PlaylistMode.Random)
      {
        UI(VaMUI.CreateInfoText(
          VaMUI.LEFT,
          "The animations will play randomly.",
          1
        ));
      }

      // DEFAULTS
      UI(VaMUI.CreateSpacer(VaMUI.LEFT));
      CreateSubHeader(VaMUI.LEFT, "Defaults");
      UI(VaMUI.CreateInfoText(
        VaMUI.LEFT,
        "New animations will use these defaults.",
        1
      ));
      UI(activePlaylist.defaultTimingModeChooser.Draw(VaMUI.LEFT));
      UI(activePlaylist.defaultWeightSlider.Draw(VaMUI.LEFT));
      if (activePlaylist.defaultTimingModeChooser.val == TimingMode.RandomDuration)
      {
        UI(activePlaylist.defaultDurationMinSlider.Draw(VaMUI.LEFT));
        UI(activePlaylist.defaultDurationMaxSlider.Draw(VaMUI.LEFT));
      }
      else
      {
        UI(activePlaylist.defaultDurationFixedSlider.Draw(VaMUI.LEFT));
      }
      UI(VaMUI.CreateButton(VaMUI.LEFT, "Apply to All", HandleApplyDefaultsToAll));

      // ACTIONS
      DrawStateActions();

      // PLAYLIST
      UI(VaMUI.CreateInfoText(
        VaMUI.RIGHT,
        "This is the playlist of animations for this layer.",
        2
      ));

      UI(VaMUI.CreateButton(VaMUI.RIGHT, "Add Current Animation", HandleAddPlaylistEntry));
      UI(VaMUI.CreateSpacer(VaMUI.RIGHT));

      for (int i = 0; i < activePlaylist.entries.Count; i++)
      {
        PlaylistEntry entry = activePlaylist.entries[i];
        string playlistMode = activePlaylist.playModeChooser.val;
        string timingMode = entry.timingModeChooser.val;
        string loopType = entry.animation.loopTypeChooser.val;

        UI(CreatePlaylistEntryContainer(playlistMode, timingMode, loopType, i + 1, VaMUI.RIGHT));
        UI(VaMUI.CreateSpacer(VaMUI.RIGHT, 10f));
        UI(VaMUI.CreateLabelWithX(VaMUI.RIGHT, $"<b>{entry.animation.name}</b>", () => { HandleDeletePlaylistEntry(entry); }));
        UI(VaMUI.CreateButtonPair(VaMUI.RIGHT, "Move Up", () => HandleMovePlaylistEntry(entry, -1), "Move Down", () => HandleMovePlaylistEntry(entry, 1)));
        UI(entry.timingModeChooser.Draw(VaMUI.RIGHT));
        if (timingMode == TimingMode.DurationFromAnimation && loopType != LoopType.PlayOnce)
        {
          UI(VaMUI.CreateInfoText(
            VaMUI.RIGHT,
            "<b>Note</b>: this animation is set to loop and will play indefinitely.",
            2
          ));
        }
        if (playlistMode == PlaylistMode.Random)
        {
          UI(entry.weightSlider.Draw(VaMUI.RIGHT));
        }
        if (timingMode == TimingMode.FixedDuration)
        {
          UI(entry.durationFixedSlider.Draw(VaMUI.RIGHT));
        }
        else if (timingMode == TimingMode.RandomDuration)
        {
          UI(entry.durationMinSlider.Draw(VaMUI.RIGHT));
          UI(entry.durationMaxSlider.Draw(VaMUI.RIGHT));
        }
        UI(VaMUI.CreateSpacer(VaMUI.RIGHT));
      }
    }

    void DrawStateActions()
    {
      UI(VaMUI.CreateSpacer(VaMUI.LEFT));
      CreateSubHeader(VaMUI.LEFT, "Actions");
      UI(VaMUI.CreateButton(VaMUI.LEFT, "On Enter State", activeState.onEnterTrigger.OpenPanel));
      UI(VaMUI.CreateButton(VaMUI.LEFT, "On Exit State", activeState.onExitTrigger.OpenPanel));
      UI(VaMUI.CreateButtonPair(VaMUI.LEFT, "Copy Actions", activeState.CopyActions, "Paste Actions", activeState.PasteActions));
    }

    void HandleNewState()
    {
      if (activeGroup == null) return;
      State state = new State(activeGroup);
      RefreshStateList();
      activeStateIdChooser.val = state.id;
    }

    void HandleDuplicateState()
    {
      if (activeState == null) return;
      State state = activeState.Clone();
      RefreshStateList();
      activeStateIdChooser.val = state.id;
    }

    void HandleRenameState(string val)
    {
      if (activeState == null) return;
      activeState.name = val;
      RefreshStateList();
      activeStateIdChooser.val = activeState.id;
    }

    void HandleDeleteState()
    {
      if (activeState == null) return;
      activeState.Dispose();
      RefreshStateList();
      VaMUtils.SelectStringChooserFirstValue(activeStateIdChooser.storable);
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
      playlist.Dispose();

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
    void BuildLayersTabUI()
    {
      CreateMainHeader(VaMUI.LEFT, "Layers");
      UI(VaMUI.CreateInfoText(
        VaMUI.LEFT,
        "A <b>Layer</b> is a set of controllers and/or morphs that are acted upon by an <b>Animation</b>. Layers allow a character to have several independently animated parts.",
        185f
      ));

      CreateBasicFunctionsUI(
        "Layer",
        activeLayer == null,
        editLayerNameInput,
        HandleNewLayer,
        HandleDuplicateLayer,
        HandleDeleteLayer
      );

      if (activeLayer == null) return;

      CreateSubHeader(VaMUI.LEFT, "Default Transition");
      UI(activeLayer.defaultTransitionDurationSlider.Draw(VaMUI.LEFT));
      UI(activeLayer.defaultTransitionEasingChooser.Draw(VaMUI.LEFT));

      CreateSubHeader(VaMUI.LEFT, "Controllers");
      UI(VaMUI.CreateButtonPair(VaMUI.LEFT, "Select All Position", () => { HandleSelectAllControllers(true, false); }, "Select All Rotation", () => { HandleSelectAllControllers(false, true); }));
      UI(VaMUI.CreateButton(VaMUI.LEFT, "Deselect All", HandleDeselectAllControllers));
      foreach (TrackedController tc in activeLayer.trackedControllers)
      {
        UI(CreateControllerSelector(tc, VaMUI.LEFT));
      }

      CreateSubHeader(VaMUI.RIGHT, "Morphs");
      UI(morphChooserUseFavoritesToggle.Draw(VaMUI.RIGHT));
      UI(VaMUI.CreateButton(VaMUI.RIGHT, "Force Refresh Morph List", () => { SetMorphChooserChoices(true); }, color: VaMUI.YELLOW));
      UI(addMorphChooser.Draw(VaMUI.RIGHT));
      SetMorphChooserChoices();
      UI(VaMUI.CreateButton(VaMUI.RIGHT, "Add Morph", HandleAddMorph));
      foreach (TrackedMorph tm in activeLayer.trackedMorphs)
      {
        UI(VaMUI.CreateLabelWithX(VaMUI.RIGHT, tm.standardName, () => { HandleDeleteMorph(tm); }));
      }
    }

    void HandleNewLayer()
    {
      Layer layer = new Layer();
      RefreshLayerList();
      activeLayerIdChooser.val = layer.id;
    }

    void HandleDuplicateLayer()
    {
      if (activeLayer == null) return;
      Layer layer = activeLayer.Clone();
      RefreshLayerList();
      activeLayerIdChooser.val = layer.id;
    }

    void HandleRenameLayer(string val)
    {
      if (activeLayer == null) return;
      activeLayer.name = val;
      RefreshLayerList();
      activeLayerIdChooser.val = activeLayer.id;
    }

    void HandleDeleteLayer()
    {
      if (activeLayer == null) return;
      activeLayer.Dispose();
      RefreshLayerList();
      VaMUtils.SelectStringChooserFirstValue(activeLayerIdChooser.storable);
    }

    void HandleSelectAllControllers(bool pos, bool rot)
    {
      foreach (TrackedController tc in activeLayer.trackedControllers)
      {
        if (pos)
        {
          tc.trackPositionToggle.val = true;
        }
        if (rot)
        {
          tc.trackRotationToggle.val = true;
        }
      }
    }

    void HandleDeselectAllControllers()
    {
      foreach (TrackedController tc in activeLayer.trackedControllers)
      {
        tc.trackPositionToggle.val = false;
        tc.trackRotationToggle.val = false;
      }
    }

    void HandleAddMorph()
    {
      if (activeLayer == null) return;
      DAZMorph morph = morphsControl.GetMorphByUid(addMorphChooser.val);
      if (morph == null) return;
      activeLayer.TrackMorph(morph);

      addMorphChooser.valNoCallback = "";
      RequestRedraw();
    }

    void HandleDeleteMorph(TrackedMorph tm)
    {
      if (activeLayer == null) return;
      activeLayer.trackedMorphs.Remove(tm);
      RequestRedraw();
    }

    void HandleToggleMorphChooserFavorites()
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
        List<DAZMorph> morphs = morphsControl.GetMorphs();
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

      bool needsRefresh = morphChooserUseFavoritesToggle.val != cachedUseFavorites;
      if (needsRefresh)
      {
        bool useFavorites = morphChooserUseFavoritesToggle.val;
        cachedUseFavorites = useFavorites;
        addMorphChooser.choices = useFavorites ? cachedFavoriteMorphChoices : cachedMorphChoices;
        addMorphChooser.displayChoices = useFavorites ? cachedFavoriteMorphDisplayChoices : cachedMorphDisplayChoices;
      }
    }


    // ================================================================================ //
    // ================================ ANIMATIONS TAB ================================ //
    // ================================================================================ //
    void BuildAnimationsTabUI()
    {
      CreateMainHeader(VaMUI.LEFT, "Animations");
      UI(VaMUI.CreateInfoText(
        VaMUI.LEFT,
        "An <b>Animation</b> defines how a <b>Layer</b> should evolve over time. An animation is composed of one or more <b>Keyframes</b> connected by tweens.",
        185f
      ));

      if (activeLayer == null)
      {
        UI(VaMUI.CreateSpacer(VaMUI.RIGHT, 50f));
        UI(VaMUI.CreateInfoText(
          VaMUI.RIGHT,
          "You must select a <b>Layer</b> before you can create any <b>Animations</b>.",
          2
        ));
        return;
      }

      CreateBasicFunctionsUI(
        "Anim.",
        activeAnimation == null,
        editAnimationNameInput,
        HandleNewAnimation,
        HandleDuplicateAnimation,
        HandleDeleteAnimation
      );

      if (activeAnimation == null) return;

      CreateSubHeader(VaMUI.RIGHT, "Animation Details");
      UI(activeAnimation.loopTypeChooser.Draw(VaMUI.RIGHT));
      UI(activeAnimation.playbackSpeedSlider.Draw(VaMUI.RIGHT));

      CreateSubHeader(VaMUI.LEFT, "Keyframe Defaults");
      UI(activeAnimation.defaultDurationSlider.Draw(VaMUI.LEFT));
      UI(activeAnimation.defaultEasingChooser.Draw(VaMUI.LEFT));

      UI(VaMUI.CreateSpacer(VaMUI.LEFT));
      CreateSubHeader(VaMUI.LEFT, "Actions");
      UI(VaMUI.CreateButton(VaMUI.LEFT, "On Enter Animation", activeAnimation.onEnterTrigger.OpenPanel));
      UI(VaMUI.CreateButton(VaMUI.LEFT, "On Animation Playing", activeAnimation.onPlayingTrigger.OpenPanel));
      UI(VaMUI.CreateButton(VaMUI.LEFT, "On Exit Animation", activeAnimation.onExitTrigger.OpenPanel));
      UI(VaMUI.CreateButtonPair(VaMUI.LEFT, "Copy Actions", activeAnimation.CopyActions, "Paste Actions", activeAnimation.PasteActions));
    }

    void HandleNewAnimation()
    {
      if (activeLayer == null) return;
      Animation animation = new Animation(activeLayer);
      RefreshAnimationList();
      activeAnimationIdChooser.val = animation.id;
    }

    void HandleDuplicateAnimation()
    {
      if (activeAnimation == null) return;
      Animation animation = activeAnimation.Clone();
      RefreshAnimationList();
      activeAnimationIdChooser.val = animation.id;
    }

    void HandleRenameAnimation(string val)
    {
      if (activeAnimation == null) return;
      activeAnimation.name = val;
      RefreshAnimationList();
      activeAnimationIdChooser.val = activeAnimation.id;
    }

    void HandleDeleteAnimation()
    {
      if (activeAnimation == null) return;
      activeAnimation.Dispose();
      RefreshAnimationList();
      VaMUtils.SelectStringChooserFirstValue(activeAnimationIdChooser.storable);
    }


    // =============================================================================== //
    // ================================ KEYFRAMES TAB ================================ //
    // =============================================================================== //
    void BuildKeyframesTabUI()
    {
      CreateMainHeader(VaMUI.LEFT, "Keyframes");
      UI(VaMUI.CreateInfoText(
        VaMUI.LEFT,
        "A <b>Keyframe</b> is a snapshot of a <b>Layer</b>'s state that records morph and controller values. An <b>Animation</b> is composed of one or more keyframes.",
        185f
      ));

      if (activeAnimation == null)
      {
        UI(VaMUI.CreateSpacer(VaMUI.RIGHT, 50f));
        UI(VaMUI.CreateInfoText(
          VaMUI.RIGHT,
          "You must select an <b>Animation</b> before you can create any <b>Keyframes</b>.",
          2
        ));
        return;
      }

      UI(VaMUI.CreateButtonPair(VaMUI.LEFT, "Preview Animation", CreatePreviewPlayer, "Stop Preview", DestroyPreviewPlayer));

      EnsureSelectedKeyframe();

      // KEYFRAME SELECTOR
      CreateSubHeader(VaMUI.LEFT, "Keyframe Selector");
      if (activeAnimation.keyframes.Count == 0)
      {
        UI(VaMUI.CreateButton(VaMUI.LEFT, "New Keyframe", HandleAddKeyframeStart));
        return;
      }
      UI(VaMUI.CreateButtonPair(VaMUI.LEFT, "<< New Keyframe", HandleAddKeyframeStart, "New Keyframe >>", HandleAddKeyframeEnd));
      UI(VaMUI.CreateButtonPair(VaMUI.LEFT, "< New Keyframe", HandleAddKeyframeBefore, "New Keyframe >", HandleAddKeyframeAfter));
      UI(VaMUI.CreateButton(VaMUI.LEFT, "Duplicate Keyframe", HandleDuplicateKeyframe));
      UI(VaMUI.CreateButtonPair(VaMUI.LEFT, "< Move Keyframe", () => { HandleMoveKeyframe(-1); }, "Move Keyframe >", () => { HandleMoveKeyframe(1); }));
      UI(CreateKeyframeSelector(activeAnimation.keyframes, activeKeyframe, VaMUI.LEFT, HandleSelectKeyframe));

      if (activeKeyframe == null) return;

      UI(activeKeyframe.labelInput.Draw(VaMUI.LEFT));

      UI(activeKeyframe.colorPicker.Draw(VaMUI.LEFT));
      UI(VaMUI.CreateButton(VaMUI.LEFT, "Apply Color", () => { RequestRedraw(); }));

      // ACTIONS
      UI(VaMUI.CreateSpacer(VaMUI.LEFT));
      CreateSubHeader(VaMUI.LEFT, "Actions");
      UI(VaMUI.CreateButton(VaMUI.LEFT, "On Enter Keyframe", activeKeyframe.onEnterTrigger.OpenPanel));
      UI(VaMUI.CreateButton(VaMUI.LEFT, "On Keyframe Playing", activeKeyframe.onPlayingTrigger.OpenPanel));
      UI(VaMUI.CreateButton(VaMUI.LEFT, "On Exit Keyframe", activeKeyframe.onExitTrigger.OpenPanel));
      UI(VaMUI.CreateButtonPair(VaMUI.LEFT, "Copy Actions", activeKeyframe.CopyActions, "Paste Actions", activeKeyframe.PasteActions));

      // KEYFRAME DETAILS
      CreateSubHeader(VaMUI.RIGHT, "Keyframe Details");
      UI(VaMUI.CreateButton(VaMUI.RIGHT, "Capture Current State", HandleCaptureKeyframe, color: VaMUI.YELLOW));
      UI(VaMUI.CreateButton(VaMUI.RIGHT, "Go To Keyframe", HandleGoToKeyframe));
      UI(VaMUI.CreateSpacer(VaMUI.RIGHT));
      UI(VaMUI.CreateButton(VaMUI.RIGHT, "Delete Keyframe", HandleDeleteKeyframe, color: VaMUI.RED));
      UI(VaMUI.CreateSpacer(VaMUI.RIGHT));

      UI(activeKeyframe.durationSlider.Draw(VaMUI.RIGHT));
      UI(activeKeyframe.easingChooser.Draw(VaMUI.RIGHT));
      UI(VaMUI.CreateSpacer(VaMUI.RIGHT));

      // MORPHS
      CreateSubHeader(VaMUI.RIGHT, "Morph Captures");
      foreach (TrackedMorph tm in activeKeyframe.animation.layer.trackedMorphs)
      {
        CapturedMorph capture = activeKeyframe.GetCapturedMorph(tm.morph.uid);
        tm.UpdateSliderToMorph();
        UI(tm.slider.Draw(VaMUI.RIGHT));
        string str = capture == null ? "<b>Val</b>: <NO DATA>" : $"<b>Val</b>: {capture.value}";
        UI(VaMUI.CreateInfoText(VaMUI.RIGHT, $"<size=24>{str}</size>", 1, background: false));
      }
      if (activeKeyframe.animation.layer.trackedMorphs.Count == 0)
      {
        UI(VaMUI.CreateInfoText(VaMUI.RIGHT, "<none>", 1, background: false));
      }
      UI(VaMUI.CreateSpacer(VaMUI.RIGHT));

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
        if (tc.trackPositionToggle.val)
        {
          posStr = capture?.position == null ? "<b>P</b>: <NO DATA>" : $"<b>P</b>: {capture.position.Value}";
        }
        if (tc.trackRotationToggle.val)
        {
          rotStr = capture?.rotation == null ? "<b>R</b>: <NO DATA>" : $"<b>R</b>: {capture.rotation.Value.eulerAngles}";
        }
        if (posStr.Length > 0 && rotStr.Length > 0)
        {
          joinStr = " ";
        }
        string str = $"<b>{nameStr}</b>\n<size=24>{posStr}{joinStr}{rotStr}</size>";
        UI(VaMUI.CreateInfoText(VaMUI.RIGHT, str, 2, background: false));
      }
      if (controllerCount == 0)
      {
        UI(VaMUI.CreateInfoText(VaMUI.RIGHT, "<none>", 1, background: false));
      }
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
      activeKeyframe.Dispose();
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
        if (tc.trackPositionToggle.val && capture?.position != null)
        {
          controllerTransform.position = mainTransform.TransformPoint(capture.position.Value);
        }
        if (tc.trackRotationToggle.val && capture?.rotation != null)
        {
          controllerTransform.rotation = mainTransform.rotation * capture.rotation.Value;
        }
      }

      foreach (TrackedMorph tm in activeKeyframe.animation.layer.trackedMorphs)
      {
        CapturedMorph capture = activeKeyframe.GetCapturedMorph(tm.morph.uid);
        if (capture != null)
        {
          tm.morph.morphValue = capture.value;
          tm.UpdateSliderToMorph();
        }
      }
    }

    void CreatePreviewPlayer()
    {
      DestroyPreviewPlayer();
      if (activeAnimation == null) return;
      playbackEnabledToggle.valNoCallback = false;
      previewAnimationPlayer = new AnimationPlayer(null);
      previewAnimationPlayer.SetAnimation(activeAnimation);
    }

    void DestroyPreviewPlayer()
    {
      if (previewAnimationPlayer == null) return;
      previewAnimationPlayer.Dispose();
      previewAnimationPlayer = null;
    }

    void UpdatePreviewAnimationPlayer()
    {
      if (previewAnimationPlayer != null && activeTab == Tabs.Keyframes && !playbackEnabledToggle.val)
      {
        previewAnimationPlayer.Update();
      }
    }


    // =========================================================================== //
    // ================================ ROLES TAB ================================ //
    // =========================================================================== //
    void BuildRolesTabUI()
    {
      CreateMainHeader(VaMUI.LEFT, "Roles");
      UI(VaMUI.CreateSpacer(VaMUI.RIGHT, 45f));

      UI(VaMUI.CreateInfoText(
        VaMUI.LEFT,
        "A <b>Message</b> is a custom event that allows communication between characters or other triggers in the scene. Messages allow external management of the character's state in a more robust way than traditional triggers. Messages are automatically exchanged between all instances of CharacterStateManager.",
        9
      ));
      UI(VaMUI.CreateSpacer(VaMUI.LEFT));
      UI(VaMUI.CreateInfoText(
        VaMUI.LEFT,
        $"<b>Roles</b> define the identity of a character when sending messages. Characters can react to messages sent by specific roles. The predefined <b>{Role.Self}</b> role allows for communication between internal states.",
        6
      ));
      UI(VaMUI.CreateSpacer(VaMUI.LEFT));
      UI(VaMUI.CreateInfoText(
        VaMUI.LEFT,
        "Default messages are automatically sent on state enter/exit. In addition, custom messages can be manually sent to this character with the <b>SendMessage</b> action, or broadcast from this character with the <b>BroadcastMessage</b> action.",
        7
      ));

      UI(Role.addRoleInput.Draw(VaMUI.RIGHT));
      UI(VaMUI.CreateButton(VaMUI.RIGHT, "Add Role", HandleAddRole));
      UI(VaMUI.CreateSpacer(VaMUI.RIGHT));

      foreach (Role role in Role.list)
      {
        if (!role.isSelf)
        {
          UI(VaMUI.CreateLabelWithX(VaMUI.RIGHT, role.name, () => { HandleRemoveRole(role); }));
          UI(role.useRoleToggle.Draw(VaMUI.RIGHT));
          UI(VaMUI.CreateSpacer(VaMUI.RIGHT));
        }
      }
    }

    void HandleAddRole()
    {
      Role.AddRole(Role.addRoleInput.val);
      Role.addRoleInput.val = "";
      RequestRedraw();
    }

    void HandleRemoveRole(Role role)
    {
      Role.RemoveRole(role);
      RequestRedraw();
    }


    // ============================================================================== //
    // ================================ MESSAGES TAB ================================ //
    // ============================================================================== //
    void BuildMessagesTabUI()
    {
      CreateMainHeader(VaMUI.LEFT, "Messages");
      UI(VaMUI.CreateSpacer(VaMUI.RIGHT, 45f));

      UI(VaMUI.CreateInfoText(
        VaMUI.LEFT,
        $"Here you can trigger character <b>States</b> based on <b>Messages</b> sent by <b>Roles</b> in your scene. Messages sent by the <b>SendMessage</b> action will appear under the <b>{Role.Self}</b> role.",
        5
      ));

      CreateSubHeader(VaMUI.RIGHT, "Listeners");
      if (activeGroup == null || activeState == null)
      {
        UI(VaMUI.CreateInfoText(
          VaMUI.RIGHT,
          "You must select a <b>Group</b> with at least one <b>State</b> to add any listeners.",
          2
        ));
        return;
      }

      UI(VaMUI.CreateSpacer(VaMUI.LEFT, 10f));
      UI(VaMUI.CreateInfoText(VaMUI.LEFT, "The following state:", 1));
      UI(VaMUI.CreateInfoText(VaMUI.LEFT, $"<b>{activeState.name}</b>", 1, background: false));
      UI(VaMUI.CreateInfoText(VaMUI.LEFT, "will be triggered when this role:", 1));
      Messages.SetRoleChooserChoices();
      UI(Messages.roleChooser.Draw(VaMUI.LEFT));
      UI(VaMUI.CreateInfoText(VaMUI.LEFT, $"sends this message:", 1));
      UI(Messages.messageTypeChooser.Draw(VaMUI.LEFT));
      if (Messages.messageTypeChooser.val == MessageType.Custom)
      {
        UI(Messages.customMessageInput.Draw(VaMUI.LEFT));
      }
      else
      {
        if (Messages.roleChooser.val == Role.Self)
        {
          Messages.SetGroupStateChooserChoices();
          UI(Messages.groupChooser.Draw(VaMUI.LEFT));
          UI(Messages.stateChooser.Draw(VaMUI.LEFT));
        }
        else
        {
          UI(Messages.groupInput.Draw(VaMUI.LEFT));
          UI(Messages.stateInput.Draw(VaMUI.LEFT));
        }
      }
      UI(VaMUI.CreateButton(VaMUI.LEFT, "Add Listener", HandleAddListener));

      UI(VaMUI.CreateInfoText(VaMUI.RIGHT, "This state will trigger when these messages are received:", 2));
      foreach (MessageListener listener in Messages.GetListenersForState(activeState))
      {
        UI(VaMUI.CreateLabelWithX(VaMUI.RIGHT, $"<size=22>{listener.text}</size>", () => { HandleDeleteListener(listener); }));
      }
    }

    void HandleAddListener()
    {
      if (activeState == null) return;
      Messages.AddListener(activeState);
      Messages.customMessageInput.val = "";
      Messages.groupInput.val = "";
      Messages.stateInput.val = "";
      Messages.groupChooser.val = "";
      Messages.stateChooser.val = "";
      RequestRedraw();
    }

    void HandleDeleteListener(MessageListener listener)
    {
      Messages.RemoveListener(listener);
      RequestRedraw();
    }


    // =================================================================================== //
    // ================================ EXPORT/IMPORT TAB ================================ //
    // =================================================================================== //
    void BuildExportImportTabUI()
    {
      CreateMainHeader(VaMUI.LEFT, "Export / Import");
      UI(VaMUI.CreateSpacer(VaMUI.RIGHT, 45f));

      UI(VaMUI.CreateInfoText(
        VaMUI.LEFT,
        "Export / Import tab",
        1
      ));
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
        tc.trackPositionToggle.storable.RegisterToggle(uid.posToggle);
        tc.trackRotationToggle.storable.RegisterToggle(uid.rotToggle);
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
          HSVColor color = keyframe.colorPicker.val;
          button.buttonColor = Color.HSVToRGB(color.H, color.S, color.V);

          RectTransform labelRect = VaMUI.InstantiateLabel(buttonRect);
          labelRect.anchorMax = new Vector2(1f, 0.6f);
          labelRect.offsetMin = new Vector2(5f, 5f);
          Text labelText = labelRect.GetComponent<Text>();
          labelText.text = $"<size=22>{keyframe.labelInput.val}</size>";
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
      public RectTransform background;
      public Text indexText;
    }
    
    private GameObject playlistEntryContainerPrefab;
    public UIDynamicPlaylistEntryContainer CreatePlaylistEntryContainer(string playlistMode, string timingMode, string loopType, int index, VaMUI.Column side)
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
        if (timingMode == TimingMode.DurationFromAnimation && loopType != LoopType.PlayOnce) height += 88f;
        if (playlistMode == PlaylistMode.Random) height += sliderHeight;
        if (timingMode == TimingMode.FixedDuration) height += sliderHeight;
        if (timingMode == TimingMode.RandomDuration) height += 2f * sliderHeight;
        uid.background.offsetMin = new Vector2(0f, -height);
        uid.gameObject.SetActive(true);
        return uid;
      }
    }


    // ============================================================================== //
    // ================================ MISC CLASSES ================================ //
    // ============================================================================== //
    public static class ActionClipboard
    {
      public static EventTrigger onEnterTrigger = null;
      public static ValueTrigger onPlayingTrigger = null;
      public static EventTrigger onExitTrigger = null;
    }
  }
}
