using UnityEngine;
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

    void UIInit()
    {
      UIBuilder.Init(this, CreateUIElement);
      RebuildUI();
      SetupCallbacks();
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

      UI(UIBuilder.CreateStringChooser(ref selectGroupStorable, UIColumn.LEFT, "Group", null, register: false));
      UI(UIBuilder.CreateStringChooser(ref selectStateStorable, UIColumn.RIGHT, "State", null, register: false));
      UI(UIBuilder.CreateStringChooser(ref selectLayerStorable, UIColumn.LEFT, "Layer", null, register: false));
      UI(UIBuilder.CreateStringChooser(ref selectAnimationStorable, UIColumn.RIGHT, "Animation", null, register: false));

      UI(UIBuilder.CreateTabBar(UIColumn.LEFT, Tabs.list, HandleTabSelect, 5));
      UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 2 * 55f));

      BuildActiveTabUI();
    }

    void SetupCallbacks()
    {
      selectGroupStorable.setCallbackFunction += HandleSelectGroup;
      selectStateStorable.setCallbackFunction += HandleSelectState;
      selectLayerStorable.setCallbackFunction += HandleSelectLayer;
      selectAnimationStorable.setCallbackFunction += HandleSelectAnimation;
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

      InvalidateUI();
    }

    void HandleSelectState(string val)
    {
      Utils.EnsureStringChooserValue(selectStateStorable, defaultToFirstChoice: true, noCallback: true);
      activeState = State.list.Find((s) => s.name == selectStateStorable.val);

      InvalidateUI();
    }

    void HandleSelectLayer(string val)
    {
      Utils.EnsureStringChooserValue(selectLayerStorable, defaultToFirstChoice: true, noCallback: true);
      activeLayer = Layer.list.Find((l) => l.name == selectLayerStorable.val);

      RefreshAnimationList();
      Utils.SelectStringChooserFirstValue(selectAnimationStorable);

      InvalidateUI();
    }

    void HandleSelectAnimation(string val)
    {
      Utils.EnsureStringChooserValue(selectAnimationStorable, defaultToFirstChoice: true, noCallback: true);
      activeAnimation = Animation.list.Find((a) => a.name == selectAnimationStorable.val);

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
    }


    // ============================================================================ //
    // ================================ GROUPS TAB ================================ //
    // ============================================================================ //
    void BuildGroupsTabUI()
    {
      UI(UIBuilder.CreateHeaderText(UIColumn.LEFT, "Groups"));
      UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 45f));

      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"A <b>Group</b> is a collection of states that operate independently of other groups. For example, you may have a primary group of main states like idle, sitting, etc, and a secondary group of gestures.",
        5
      ));

      UI(UIBuilder.CreateButton(UIColumn.LEFT, "New Group", HandleNewGroup));

      if (activeGroup == null)
      {
        return;
      }

      UI(UIBuilder.CreateSpacer(UIColumn.LEFT));
      var deleteButton = UIBuilder.CreateButton(UIColumn.LEFT, "Delete Group", HandleDeleteGroup);
      deleteButton.buttonColor = UIColor.RED;
      UI(deleteButton);
    }

    void HandleNewGroup()
    {
      Group group = new Group();
      RefreshGroupList();
      selectGroupStorable.val = group.name;
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
      UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 45f));

      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"A <b>State</b> defines what a character is currently doing (idle, sitting, etc). A state assigns animations to layers that can be played either sequentially or randomly. For example, a dance may be composed of sequential animations, or an idle may be composed of random animations.",
        7
      ));

      if (activeGroup == null)
      {
        UI(UIBuilder.CreateInfoTextNoScroll(
          UIColumn.RIGHT,
          @"You must select a <b>Group</b> before you can create any <b>States</b>.",
          2
        ));
        return;
      }

      UI(UIBuilder.CreateButton(UIColumn.LEFT, "New State", HandleNewState));

      if (activeState == null)
      {
        return;
      }

      UI(UIBuilder.CreateSpacer(UIColumn.LEFT));
      var deleteButton = UIBuilder.CreateButton(UIColumn.LEFT, "Delete State", HandleDeleteState);
      deleteButton.buttonColor = UIColor.RED;
      UI(deleteButton);
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
    void BuildLayersTabUI()
    {
      UI(UIBuilder.CreateHeaderText(UIColumn.LEFT, "Layers"));
      UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 45f));

      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"A <b>Layer</b> is a set of controls and/or morphs that are acted upon by an animation. Layers allow a character to have several independently animated parts.",
        5
      ));

      UI(UIBuilder.CreateButton(UIColumn.LEFT, "New Layer", HandleNewLayer));

      if (activeLayer == null)
      {
        return;
      }

      UI(UIBuilder.CreateSpacer(UIColumn.LEFT));
      var deleteButton = UIBuilder.CreateButton(UIColumn.LEFT, "Delete Layer", HandleDeleteLayer);
      deleteButton.buttonColor = UIColor.RED;
      UI(deleteButton);
    }

    void HandleNewLayer()
    {
      Layer layer = new Layer();
      RefreshLayerList();
      selectLayerStorable.val = layer.name;
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


    // ================================================================================ //
    // ================================ ANIMATIONS TAB ================================ //
    // ================================================================================ //
    void BuildAnimationsTabUI()
    {
      UI(UIBuilder.CreateHeaderText(UIColumn.LEFT, "Animations"));
      UI(UIBuilder.CreateSpacer(UIColumn.RIGHT, 45f));

      UI(UIBuilder.CreateInfoTextNoScroll(
        UIColumn.LEFT,
        @"An <b>Animation</b> defines what a layer should do. An animation is composed of one or more <b>Keyframes</b>, which are snapshots of a layer's state.",
        4
      ));

      if (activeLayer == null)
      {
        UI(UIBuilder.CreateInfoTextNoScroll(
          UIColumn.RIGHT,
          @"You must select a <b>Layer</b> before you can create any <b>Animations</b>.",
          2
        ));
        return;
      }

      UI(UIBuilder.CreateButton(UIColumn.LEFT, "New Animation", HandleNewAnimation));

      if (activeAnimation == null)
      {
        return;
      }

      UI(UIBuilder.CreateSpacer(UIColumn.LEFT));
      var deleteButton = UIBuilder.CreateButton(UIColumn.LEFT, "Delete Animation", HandleDeleteAnimation);
      deleteButton.buttonColor = UIColor.RED;
      UI(deleteButton);
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
    }
  }
}
