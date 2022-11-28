using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using VaMUtils;
using System;

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
      public const string Transitions = "Transitions";
      public const string Triggers = "Triggers";

      public static readonly string[] list = new string[] { Info, Groups, States, Layers, Animations, Transitions, Triggers };
    }

    bool uiNeedsRebuilt = false;
    List<object> uiItems = new List<object>();

    GameObject tabBarPrefab;
    string activeTab;

    JSONStorableBool playbackEnabledStorable;

    JSONStorableStringChooser selectGroupStorable;
    JSONStorableStringChooser selectStateStorable;
    JSONStorableStringChooser selectLayerStorable;
    JSONStorableStringChooser selectAnimationStorable;

    Group activeGroup = null;
    State activeState = null;
    Layer activeLayer = null;
    Animation activeAnimation = null;

    JSONStorableString groupNameStorable;
    JSONStorableString stateNameStorable;
    JSONStorableString layerNameStorable;
    JSONStorableString animationNameStorable;

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
      // uiNeedsRebuilt = true;
      RebuildUI();
    }

    void UI(object item)
    {
      uiItems.Add(item);
    }

    void RebuildUI()
    {
      UIBuilder.RemoveUIElements(ref uiItems);

      UI(UIBuilder.CreateToggle(ref playbackEnabledStorable, UIColumn.LEFT, "Playback Enabled", true));
      UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 50f));

      UI(UIBuilder.CreateStringChooser(ref selectGroupStorable, UIColumn.LEFT, "Group", null, callback: HandleSelectGroup, register: false));
      UI(UIBuilder.CreateStringChooser(ref selectStateStorable, UIColumn.RIGHT, "State", null, callback: HandleSelectState, register: false));
      UI(UIBuilder.CreateStringChooser(ref selectLayerStorable, UIColumn.LEFT, "Layer", null, callback: HandleSelectLayer, register: false));
      UI(UIBuilder.CreateStringChooser(ref selectAnimationStorable, UIColumn.RIGHT, "Animation", null, callback: HandleSelectAnimation, register: false));

      UI(UIBuilder.CreateTabBar(ref tabBarPrefab, UIColumn.LEFT, Tabs.list, HandleTabSelect, 5));
      UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 2 * 55f));

      BuildActiveTabUI();
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
      Utils.EnsureStringChooserValue(selectGroupStorable, defaultToFirstChoice: true, noCallback: true);
      activeGroup = Group.list.Find((g) => g.name == selectGroupStorable.val);

      RefreshStateList();
      Utils.SelectStringChooserFirstValue(selectStateStorable);

      if (groupNameStorable != null)
      {
        groupNameStorable.valNoCallback = activeGroup?.name ?? "";
      }

      InvalidateUI();
    }

    void HandleSelectState(string val)
    {
      Utils.EnsureStringChooserValue(selectStateStorable, defaultToFirstChoice: true, noCallback: true);
      activeState = State.list.Find((s) => s.name == selectStateStorable.val);

      if (stateNameStorable != null)
      {
        stateNameStorable.valNoCallback = activeState?.name ?? "";
      }

      InvalidateUI();
    }

    void HandleSelectLayer(string val)
    {
      Utils.EnsureStringChooserValue(selectLayerStorable, defaultToFirstChoice: true, noCallback: true);
      activeLayer = Layer.list.Find((l) => l.name == selectLayerStorable.val);

      RefreshAnimationList();
      Utils.SelectStringChooserFirstValue(selectAnimationStorable);

      if (layerNameStorable != null)
      {
        layerNameStorable.valNoCallback = activeLayer?.name ?? "";
      }

      InvalidateUI();
    }

    void HandleSelectAnimation(string val)
    {
      Utils.EnsureStringChooserValue(selectAnimationStorable, defaultToFirstChoice: true, noCallback: true);
      activeAnimation = Animation.list.Find((a) => a.name == selectAnimationStorable.val);

      if (animationNameStorable != null)
      {
        animationNameStorable.valNoCallback = activeAnimation?.name ?? "";
      }

      InvalidateUI();
    }

    void RefreshGroupList()
    {
      List<string> choices = new List<string>();
      foreach (Group group in Group.list)
      {
        choices.Add(group.name);
      }
      choices.Sort();
      selectGroupStorable.choices = choices;

      InvalidateUI();
    }

    void RefreshStateList()
    {
      List<string> choices = new List<string>();
      if (activeGroup != null)
      {
        foreach (State state in activeGroup.states)
        {
          choices.Add(state.name);
        }
        choices.Sort();
      }
      selectStateStorable.choices = choices;

      InvalidateUI();
    }

    void RefreshLayerList()
    {
      List<string> choices = new List<string>();
      foreach (Layer layer in Layer.list)
      {
        choices.Add(layer.name);
      }
      choices.Sort();
      selectLayerStorable.choices = choices;

      InvalidateUI();
    }

    void RefreshAnimationList()
    {
      List<string> choices = new List<string>();
      if (activeLayer != null)
      {
        foreach (Animation animation in activeLayer.animations)
        {
          choices.Add(animation.name);
        }
        choices.Sort();
      }
      selectAnimationStorable.choices = choices;

      InvalidateUI();
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
        UIDynamicButton deleteButton = UIBuilder.CreateButton(UIColumn.RIGHT, $"Delete {category}", deleteHandler);
        deleteButton.buttonColor = UIColor.RED;
        UI(deleteButton);
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
      UI(UIBuilder.CreateHeaderText(UIColumn.LEFT, "Info"));
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
      UI(UIBuilder.CreateHeaderText(UIColumn.LEFT, "Groups"));
      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"A <b>Group</b> is a collection of states that operate independently of other groups. For example, you may have a primary group of main states like idle, sitting, etc, and a secondary group of gestures.",
        185f
      ));

      CreateBasicFunctionsUI(
        "Group",
        activeGroup == null,
        () => { UI(UIBuilder.CreateOnelineTextInput(ref groupNameStorable, UIColumn.RIGHT, "Name", activeGroup?.name ?? "", callback: HandleRenameGroup)); },
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
      selectGroupStorable.val = group.name;
    }

    void HandleDuplicateGroup()
    {
      if (activeGroup != null)
      {
        Group group = activeGroup.Clone();
        RefreshGroupList();
        selectGroupStorable.val = group.name;
      }
    }

    void HandleRenameGroup(string val)
    {
      if (activeGroup != null)
      {
        activeGroup.SetNameUnique(val);
        RefreshGroupList();
        selectGroupStorable.val = activeGroup.name;
      }
    }

    void HandleDeleteGroup()
    {
      if (activeGroup != null)
      {
        activeGroup.Delete();
        RefreshGroupList();
        Utils.SelectStringChooserFirstValue(selectGroupStorable);
      }
    }


    // ============================================================================ //
    // ================================ STATES TAB ================================ //
    // ============================================================================ //
    void BuildStatesTabUI()
    {
      UI(UIBuilder.CreateHeaderText(UIColumn.LEFT, "States"));
      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"A <b>State</b> defines what a character is currently doing (idle, sitting, etc). A state assigns animations to layers that can be played either sequentially or randomly.",
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
        () => { UI(UIBuilder.CreateOnelineTextInput(ref stateNameStorable, UIColumn.RIGHT, "Name", activeState?.name ?? "", callback: HandleRenameState)); },
        HandleNewState,
        HandleDuplicateState,
        HandleDeleteState
      );

      if (activeState == null) return;


      CreateBottomPadding();
    }

    void HandleNewState()
    {
      if (activeGroup != null)
      {
        State state = new State(activeGroup);
        RefreshStateList();
        selectStateStorable.val = state.name;
      }
    }

    void HandleDuplicateState()
    {
      if (activeState != null)
      {
        State state = activeState.Clone();
        RefreshStateList();
        selectStateStorable.val = state.name;
      }
    }

    void HandleRenameState(string val)
    {
      if (activeState != null)
      {
        activeState.SetNameUnique(val);
        RefreshStateList();
        selectStateStorable.val = activeState.name;
      }
    }

    void HandleDeleteState()
    {
      if (activeState != null)
      {
        activeState.Delete();
        RefreshStateList();
        Utils.SelectStringChooserFirstValue(selectStateStorable);
      }
    }


    // ============================================================================ //
    // ================================ LAYERS TAB ================================ //
    // ============================================================================ //
    JSONStorableStringChooser morphChooserStorable;
    JSONStorableBool morphChooserUseFavoritesStorable;

    void BuildLayersTabUI()
    {
      UI(UIBuilder.CreateHeaderText(UIColumn.LEFT, "Layers"));
      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"A <b>Layer</b> is a set of controls and/or morphs that are acted upon by an animation. Layers allow a character to have several independently animated parts.",
        185f
      ));

      CreateBasicFunctionsUI(
        "Layer",
        activeLayer == null,
        () => { UI(UIBuilder.CreateOnelineTextInput(ref layerNameStorable, UIColumn.RIGHT, "Name", activeLayer?.name ?? "", callback: HandleRenameLayer)); },
        HandleNewLayer,
        HandleDuplicateLayer,
        HandleDeleteLayer
      );

      if (activeLayer == null) return;

      UI(UIBuilder.CreateHeaderText(UIColumn.LEFT, "Controllers", 38f));
      UI(UIBuilder.CreateButtonPair(UIColumn.LEFT, "Select All Position", () => { HandleSelectAllControllers(true, false); }, "Select All Rotation", () => { HandleSelectAllControllers(false, true); }));
      UI(UIBuilder.CreateButton(UIColumn.LEFT, "Deselect All", HandleDeselectAllControllers));
      foreach (TrackedControllerStorable storable in activeLayer.trackedControllers)
      {
        UI(CreateControllerSelector(storable, UIColumn.LEFT, callback: HandleControllerSelection));
      }

      UI(UIBuilder.CreateHeaderText(UIColumn.RIGHT, "Morphs", 38f));
      UI(UIBuilder.CreateToggle(ref morphChooserUseFavoritesStorable, UIColumn.RIGHT, "Favorites Only", true, callback: HandleToggleMorphChooserFavorites, register: false));
      UIDynamicButton refreshMorphButton = UIBuilder.CreateButton(UIColumn.RIGHT, "Force Refresh Morph List", () => { SetMorphChooserChoices(true); });
      refreshMorphButton.buttonColor = UIColor.YELLOW;
      UI(refreshMorphButton);
      UI(UIBuilder.CreateStringChooser(ref morphChooserStorable, UIColumn.RIGHT, "Select Morph", filterable: true, noDefaultSelection: true, register: false));
      SetMorphChooserChoices();
      UI(UIBuilder.CreateButton(UIColumn.RIGHT, "Add Morph", HandleAddMorph));
      foreach (DAZMorph morph in activeLayer.trackedMorphs)
      {
        UI(UIBuilder.CreateLabelWithX(UIColumn.RIGHT, morph.displayName, () => { HandleDeleteMorph(morph); }));
      }


      CreateBottomPadding();
    }

    void HandleNewLayer()
    {
      try
      {
        Layer layer = new Layer();
        RefreshLayerList();
        selectLayerStorable.val = layer.name;
      }
      catch (Exception e)
      {
        SuperController.LogError(e.ToString());
      }
    }

    void HandleDuplicateLayer()
    {
      if (activeLayer != null)
      {
        Layer layer = activeLayer.Clone();
        RefreshLayerList();
        selectLayerStorable.val = layer.name;
      }
    }

    void HandleRenameLayer(string val)
    {
      if (activeLayer != null)
      {
        activeLayer.SetNameUnique(val);
        RefreshLayerList();
        selectLayerStorable.val = activeLayer.name;
      }
    }

    void HandleDeleteLayer()
    {
      if (activeLayer != null)
      {
        activeLayer.Delete();
        RefreshLayerList();
        Utils.SelectStringChooserFirstValue(selectLayerStorable);
      }
    }

    void HandleControllerSelection(FreeControllerV3 controller, bool posValue, bool rotValue)
    {
      // Nothing for now
    }

    void HandleSelectAllControllers(bool pos, bool rot)
    {
      foreach (TrackedControllerStorable storable in activeLayer.trackedControllers)
      {
        if (pos)
        {
          storable.trackPositionStorable.val = true;
        }
        if (rot)
        {
          storable.trackRotationStorable.val = true;
        }
      }
    }

    void HandleDeselectAllControllers()
    {
      foreach (TrackedControllerStorable storable in activeLayer.trackedControllers)
      {
        storable.trackPositionStorable.val = false;
        storable.trackRotationStorable.val = false;
      }
    }

    void HandleAddMorph()
    {
      if (activeLayer == null || morphChooserStorable == null) return;
      DAZMorph morph = geometry.morphsControlUI.GetMorphByDisplayName(morphChooserStorable.val);
      if (morph == null) return;
      if (activeLayer.trackedMorphs.Contains(morph)) return;
      activeLayer.trackedMorphs.Add(morph);
      activeLayer.trackedMorphs.Sort((a, b) => String.Compare(a.displayName, b.displayName));

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

    List<string> cachedChoices = null;
    List<string> cachedFavoritesChoices = null;
    bool? cachedUseFavorites = null;
    void SetMorphChooserChoices(bool force = false)
    {
      if (morphChooserStorable == null || morphChooserUseFavoritesStorable == null) return;
      if (force || cachedChoices == null || cachedFavoritesChoices == null)
      {
        cachedChoices = new List<string>();
        cachedFavoritesChoices = new List<string>();
        List<DAZMorph> morphs = geometry.morphsControlUI.GetMorphs();
        foreach (DAZMorph morph in morphs)
        {
          cachedChoices.Add(morph.displayName);
          if (morph.favorite)
          {
            cachedFavoritesChoices.Add(morph.displayName);
          }
        }
        cachedChoices.Sort();
        cachedFavoritesChoices.Sort();
        cachedUseFavorites = null;
      }

      bool needsRefresh = morphChooserUseFavoritesStorable.val != cachedUseFavorites;
      if (needsRefresh)
      {
        morphChooserStorable.choices = morphChooserUseFavoritesStorable.val ? cachedFavoritesChoices : cachedChoices;
        cachedUseFavorites = morphChooserUseFavoritesStorable.val;
      }
    }


    // ================================================================================ //
    // ================================ ANIMATIONS TAB ================================ //
    // ================================================================================ //
    void BuildAnimationsTabUI()
    {
      UI(UIBuilder.CreateHeaderText(UIColumn.LEFT, "Animations"));
      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"An <b>Animation</b> defines what a layer should do. An animation is composed of one or more <b>Keyframes</b>, which are snapshots of a layer's state.",
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
        "Animation",
        activeAnimation == null,
        () => { UI(UIBuilder.CreateOnelineTextInput(ref animationNameStorable, UIColumn.RIGHT, "Name", activeAnimation?.name ?? "", callback: HandleRenameAnimation)); },
        HandleNewAnimation,
        HandleDuplicateAnimation,
        HandleDeleteAnimation
      );

      if (activeAnimation == null) return;


      CreateBottomPadding();
    }

    void HandleNewAnimation()
    {
      if (activeLayer != null)
      {
        Animation animation = new Animation(activeLayer);
        RefreshAnimationList();
        selectAnimationStorable.val = animation.name;
      }
    }

    void HandleDuplicateAnimation()
    {
      if (activeAnimation != null)
      {
        Animation animation = activeAnimation.Clone();
        RefreshAnimationList();
        selectAnimationStorable.val = animation.name;
      }
    }

    void HandleRenameAnimation(string val)
    {
      if (activeAnimation != null)
      {
        activeAnimation.SetNameUnique(val);
        RefreshAnimationList();
        selectAnimationStorable.val = activeAnimation.name;
      }
    }

    void HandleDeleteAnimation()
    {
      if (activeAnimation != null)
      {
        activeAnimation.Delete();
        RefreshAnimationList();
        Utils.SelectStringChooserFirstValue(selectAnimationStorable);
      }
    }


    // ================================================================================= //
    // ================================ TRANSITIONS TAB ================================ //
    // ================================================================================= //
    void BuildTransitionsTabUI()
    {
      UI(UIBuilder.CreateHeaderText(UIColumn.LEFT, "Transitions"));
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
      UI(UIBuilder.CreateHeaderText(UIColumn.LEFT, "Triggers"));
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
    public class UIDynamicControllerSelector : UIDynamicBase
    {
      public Text label;
      public Toggle posToggle;
      public Toggle rotToggle;
    }

    private GameObject controllerSelectorPrefab;
    public UIDynamicControllerSelector CreateControllerSelector(TrackedControllerStorable storable, UIColumn side, TrackedControllerStorable.SetValueCallback callback = null)
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
          storable.setCallbackFunction = callback;
        }
        Transform t = CreateUIElement(controllerSelectorPrefab.transform, side == UIColumn.RIGHT);
        UIDynamicControllerSelector uid = t.GetComponent<UIDynamicControllerSelector>();
        uid.label.text = storable.controller.name;
        storable.trackPositionStorable.RegisterToggle(uid.posToggle);
        storable.trackRotationStorable.RegisterToggle(uid.rotToggle);
        uid.gameObject.SetActive(true);
        return uid;
      }
    }
  }
}
