﻿/* /////////////////////////////////////////////////////////////////////////////////////////////////
Original utils 2021-03-13 by MacGruber
Collection of various utility functions.
https://www.patreon.com/MacGruber_Laboratory
(Note from ThatsLewd: seriously, support MacGruber -- they have done a ton for the VaM community)

Licensed under CC BY-SA after EarlyAccess ended. (see https://creativecommons.org/licenses/by-sa/4.0/)

Massively reworked + expanded in 2022 by ThatsLewd to fit my workflow
///////////////////////////////////////////////////////////////////////////////////////////////// */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using AssetBundles;
using SimpleJSON;

namespace VaMLib
{
  // ================================================================================================== //
  // ========================================== ENUMS/CONSTS ========================================== //
  // ================================================================================================== //
  public static partial class VaMUI
  {
    // Columns
    public readonly static Column LEFT = Column.LEFT;
    public readonly static Column RIGHT = Column.RIGHT;

    // Colors
    public readonly static Color GREEN = new Color(0.55f, 1.0f, 0.55f);
    public readonly static Color RED = new Color(1.0f, 0.5f, 0.5f);
    public readonly static Color BLUE = new Color(0.5f, 0.65f, 1.0f);
    public readonly static Color YELLOW = new Color(1.0f, 1.0f, 0.6f);
    public readonly static Color WHITE = new Color(1.0f, 1.0f, 1.0f);
    public readonly static Color BLACK = new Color(0.0f, 0.0f, 0.0f);
    public readonly static Color TRANSPARENT = new Color(0.0f, 0.0f, 0.0f, 0.0f);
  }


  // ============================================================================================== //
  // ========================================== UI UTILS ========================================== //
  // ============================================================================================== //
  // Usage:
  // - In script Init(), call:
  //       VaMUI.Init(this, CreateUIElement);
  // - In script OnDestroy(), call:
  //       VaMUI.Destroy();
  //
  //
  // Note: Many CreateXXX methods have many optional arguments.
  // You are HIGHLY encouraged to use C# named params for your own sanity.
  //

  public static partial class VaMUI
  {
    public static MVRScript script { get; private set; }
    private static CreateUIElement createUIElement;
    public static bool initialized { get; private set; }

    public static void Init(MVRScript script, CreateUIElement createUIElementCallback)
    {
      VaMUI.script = script;
      VaMUI.createUIElement = createUIElementCallback;
      VaMUI.initialized = true;
    }

    public static void Destroy()
    {
      VaMUtils.SafeDestroy(ref customOnelineTextInputPrefab);
      VaMUtils.SafeDestroy(ref customLabelWithXPrefab);
      VaMUtils.SafeDestroy(ref customButtonPairPrefab);
      VaMUtils.SafeDestroy(ref customInfoTextPrefab);
      VaMUtils.SafeDestroy(ref customSliderPrefab);
      VaMUtils.SafeDestroy(ref customHorizontalLinePrefab);
      VaMUtils.SafeDestroy(ref backgroundElementPrefab);
      VaMUtils.SafeDestroy(ref labelElementPrefab);
      VaMUtils.SafeDestroy(ref textFieldElementPrefab);
      VaMUtils.SafeDestroy(ref buttonElementPrefab);
    }

    // ================ CreateAction ================ //
    // Create an action that other objects in the scene can use
    public static JSONStorableAction CreateAction(string name, JSONStorableAction.ActionCallback callback)
    {
      JSONStorableAction storable = new JSONStorableAction(name, callback);
      script.RegisterAction(storable);
      return storable;
    }

    // ================ CreateBoolAction ================ //
    // Create an action that other objects in the scene can use
    public static JSONStorableBool CreateBoolAction(string name, bool defaultValue, JSONStorableBool.SetBoolCallback callback)
    {
      JSONStorableBool storable = new JSONStorableBool(name, defaultValue, callback);
      storable.isStorable = storable.isRestorable = false;
      script.RegisterBool(storable);
      return storable;
    }

    // ================ CreateFloatAction ================ //
    // Create an action that other objects in the scene can use
    public static JSONStorableFloat CreateFloatAction(string name, float defaultValue, float minValue, float maxValue, JSONStorableFloat.SetFloatCallback callback)
    {
      JSONStorableFloat storable = new JSONStorableFloat(name, defaultValue, callback, minValue, maxValue, false, true);
      storable.isStorable = storable.isRestorable = false;
      script.RegisterFloat(storable);
      return storable;
    }

    // ================ CreateStringAction ================ //
    // Create an action that other objects in the scene can use
    public static JSONStorableString CreateStringAction(string name, string defaultValue, JSONStorableString.SetStringCallback callback)
    {
      JSONStorableString storable = new JSONStorableString(name, defaultValue, callback);
      storable.isStorable = storable.isRestorable = false;
      script.RegisterString(storable);
      return storable;
    }

    // ================ CreateButton ================ //
    // Create default VaM Button
    public static UIDynamicButton CreateButton(Column side, string label, UnityAction callback = null, Color? color = null)
    {
      UIDynamicButton button = script.CreateButton(label, side == Column.RIGHT);
      if (callback != null)
      {
        button.button.onClick.AddListener(callback);
      }
      if (color != null)
      {
        button.buttonColor = color.Value;
      }
      return button;
    }

    // ================ CreateButtonPair ================ //
    // Create two buttons on one line
    private static GameObject customButtonPairPrefab;
    public static UIDynamicButtonPair CreateButtonPair(Column side, string leftLabel, UnityAction leftCallback, string rightLabel, UnityAction rightCallback, Color? leftColor = null, Color? rightColor = null)
    {
      if (customButtonPairPrefab == null)
      {
        UIDynamicButtonPair uid = CreateUIDynamicPrefab<UIDynamicButtonPair>("ButtonPair", 50f);
        customButtonPairPrefab = uid.gameObject;

        RectTransform leftButtonRect = InstantiateButton(uid.transform);
        leftButtonRect.anchorMax = new Vector2(0.5f, 1f);
        leftButtonRect.offsetMax = new Vector2(-5f, 0f);
        UIDynamicButton leftButton = leftButtonRect.GetComponent<UIDynamicButton>();
        leftButton.buttonText.text = "";

        RectTransform rightButtonRect = InstantiateButton(uid.transform);
        rightButtonRect.anchorMin = new Vector2(0.5f, 0f);
        rightButtonRect.offsetMin = new Vector2(5f, 0f);
        UIDynamicButton rightButton = rightButtonRect.GetComponent<UIDynamicButton>();
        rightButton.buttonText.text = "";

        uid.leftButton = leftButton;
        uid.rightButton = rightButton;
      }
      {
        Transform t = createUIElement(customButtonPairPrefab.transform, side == Column.RIGHT);
        UIDynamicButtonPair uid = t.GetComponent<UIDynamicButtonPair>();
        uid.leftButton.buttonText.text = leftLabel;
        uid.leftButton.button.onClick.AddListener(leftCallback);
        if (leftColor != null)
        {
          uid.leftButton.buttonColor = leftColor.Value;
        }
        uid.rightButton.buttonText.text = rightLabel;
        uid.rightButton.button.onClick.AddListener(rightCallback);
        if (rightColor != null)
        {
          uid.rightButton.buttonColor = rightColor.Value;
        }
        uid.gameObject.SetActive(true);
        return uid;
      }
    }

    // ================ CreateInfoText ================ //
    // Create an info textbox
    private static GameObject customInfoTextPrefab;
    public static UIDynamicInfoText CreateInfoText(Column side, string text, float height, bool background = true)
    {
      if (customInfoTextPrefab == null)
      {
        UIDynamicInfoText uid = CreateUIDynamicPrefab<UIDynamicInfoText>("InfoText", 120f);
        customInfoTextPrefab = uid.gameObject;

        RectTransform backgroundRect = InstantiateBackground(uid.transform);

        RectTransform textRect = InstantiateLabel(uid.transform);
        textRect.name = "Text";
        textRect.offsetMin = new Vector2(8f, 4f);
        textRect.offsetMax = new Vector2(-8f, -4f);
        Text textComponent = textRect.GetComponent<Text>();
        textComponent.alignment = TextAnchor.UpperLeft;
        textComponent.text = "";

        uid.bgImage = backgroundRect.GetComponent<Image>();
        uid.textRect = textRect;
        uid.text = textComponent;
      }
      {
        Transform t = createUIElement(customInfoTextPrefab.transform, side == Column.RIGHT);
        UIDynamicInfoText uid = t.GetComponent<UIDynamicInfoText>();
        uid.text.text = text;
        uid.layout.minHeight = height;
        uid.layout.preferredHeight = height;
        if (!background)
        {
          uid.bgImage.color = TRANSPARENT;
          uid.textRect.offsetMin = new Vector2(8f, 0f);
          uid.textRect.offsetMax = new Vector2(-8f, 0f);
        }
        uid.gameObject.SetActive(true);
        return uid;
      }
    }

    // Create an info textbox with a specified number of lines (default text size only)
    public static UIDynamicInfoText CreateInfoText(Column side, string text, int lines, bool background = true)
    {
      return CreateInfoText(side, text, lines * 32f + 8f, background);
    }

    // Creates a one-line header with automatic styling
    public static UIDynamicInfoText CreateHeaderText(Column side, string text, float size = 50f)
    {
      UIDynamicInfoText uid = CreateInfoText(side, $"<size={size * 0.85f}><b>{text}</b></size>", size, background: false);
      return uid;
    }

    // ================ CreateLabelWithX ================ //
    // Create label that has an X button on the right side
    private static GameObject customLabelWithXPrefab;
    public static UIDynamicLabelWithX CreateLabelWithX(Column side, string label, UnityAction callback)
    {
      if (customLabelWithXPrefab == null)
      {
        UIDynamicLabelWithX uid = CreateUIDynamicPrefab<UIDynamicLabelWithX>("LabelWithX", 50f);
        customLabelWithXPrefab = uid.gameObject;

        RectTransform background = InstantiateBackground(uid.transform);
        Image bgImage = background.GetComponent<Image>();
        bgImage.color = new Color(1f, 1f, 1f, 0.35f);

        RectTransform labelRect = InstantiateLabel(uid.transform);
        Text labelText = labelRect.GetComponent<Text>();
        labelText.text = "";

        RectTransform buttonRect = InstantiateButton(uid.transform);
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.offsetMin = new Vector2(-50f, 0f);
        UIDynamicButton button = buttonRect.GetComponent<UIDynamicButton>();
        button.buttonText.text = "X";

        uid.label = labelText;
        uid.button = button;
      }
      {
        Transform t = createUIElement(customLabelWithXPrefab.transform, side == Column.RIGHT);
        UIDynamicLabelWithX uid = t.GetComponent<UIDynamicLabelWithX>();
        uid.label.text = label;
        uid.button.button.onClick.AddListener(callback);
        uid.gameObject.SetActive(true);
        return uid;
      }
    }

    // ================ CreateTabBar ================ //
    // Create a list of buttons that spans both columns
    // NOTE that this creates a prefab, which you should clean up when your script exits
    // NOTE that this also means the tab bar is not dynamic -- it is setup once and the same prefab is reused
    public static UIDynamicTabBar CreateTabBar(ref GameObject prefabReference, Column anchorSide, string[] menuItems, UnityAction<string> callback, int tabsPerRow = 6)
    {
      if (prefabReference == null)
      {
        const float rowHeight = 50f;
        const float rowWidth = 1060f;
        const float spacing = 5f;

        int numRows = Mathf.CeilToInt((float)menuItems.Length / (float)tabsPerRow);
        float totalHeight = numRows * (rowHeight + spacing);
        float tabWidth = ((rowWidth + spacing) / tabsPerRow) - spacing;
        float tabHeight = rowHeight;

        UIDynamicTabBar uid = CreateUIDynamicPrefab<UIDynamicTabBar>("TabBar", 0f);
        prefabReference = uid.gameObject;

        uid.layout.minHeight = uid.layout.preferredHeight = totalHeight;
        uid.layout.minWidth = uid.layout.preferredWidth = rowWidth;

        for (int i = 0; i < menuItems.Length; i++)
        {
          int col = i % tabsPerRow;
          int row = i / tabsPerRow;

          float xOffset = col * (tabWidth + spacing);
          if (anchorSide == Column.RIGHT)
          {
            xOffset -= rowWidth / 2 + 9f;
          }
          float yOffset = -row * (rowHeight + spacing);

          RectTransform tabRect = InstantiateButton(prefabReference.transform);
          tabRect.name = "TabButton";
          tabRect.anchorMin = new Vector2(0f, 1f);
          tabRect.anchorMax = new Vector2(0f, 1f);
          tabRect.offsetMin = new Vector2(xOffset, yOffset - tabHeight);
          tabRect.offsetMax = new Vector2(xOffset + tabWidth, yOffset);

          UIDynamicButton tabButton = tabRect.GetComponent<UIDynamicButton>();
          uid.buttons.Add(tabButton);
          tabButton.buttonText.text = menuItems[i];
        }
      }
      {
        Transform t = createUIElement(prefabReference.transform, anchorSide == Column.RIGHT);
        UIDynamicTabBar uid = t.GetComponent<UIDynamicTabBar>();
        for (int i = 0; i < uid.buttons.Count; i++)
        {
          string item = menuItems[i];
          uid.buttons[i].button.onClick.AddListener(
            () => { callback(item); }
          );
        }
        uid.gameObject.SetActive(true);
        return uid;
      }
    }

    // ================ CreateSpacer ================ //
    // Create spacer
    public static UIDynamic CreateSpacer(Column side, float height = 20f)
    {
      UIDynamic spacer = script.CreateSpacer(side == Column.RIGHT);
      spacer.height = height;
      return spacer;
    }

    // ================ CreateHorizontalLine ================ //
    // Create a horizontal line
    private static GameObject customHorizontalLinePrefab;
    public static UIDynamicHorizontalLine CreateHorizontalLine(Column side)
    {
      if (customHorizontalLinePrefab == null)
      {
        UIDynamicHorizontalLine uid = CreateUIDynamicPrefab<UIDynamicHorizontalLine>("HorizontalLine", 0f);
        customHorizontalLinePrefab = uid.gameObject;

        RectTransform background = InstantiateBackground(uid.transform);
        background.offsetMin = new Vector2(0f, -2f);
        background.offsetMax = new Vector2(0f, 2f);
        Image bgImage = background.GetComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.8f);
      }
      {
        Transform t = createUIElement(customHorizontalLinePrefab.transform, side == Column.RIGHT);
        UIDynamicHorizontalLine uid = t.GetComponent<UIDynamicHorizontalLine>();
        uid.gameObject.SetActive(true);
        return uid;
      }
    }

    // ================ CreateToggle ================ //
    // Create default VaM toggle
    public static VaMToggle CreateToggle(string label, bool defaultValue, UnityAction<bool> callback = null, UnityAction callbackNoVal = null, bool register = false)
    {
      JSONStorableBool storable = new JSONStorableBool(label, defaultValue);
      if (register)
      {
        storable.storeType = JSONStorableParam.StoreType.Full;
        script.RegisterBool(storable);
      }
      if (callback != null || callbackNoVal != null)
      {
        storable.setCallbackFunction = (bool val) => { callback?.Invoke(val); callbackNoVal?.Invoke(); };
      }
      return new VaMToggle() { storable = storable };
    }

    public class VaMToggle
    {
      public JSONStorableBool storable;
      public bool val { get { return storable.val; } set { storable.val = value; }}
      public bool valNoCallback { set { storable.valNoCallback = value; }}
      public UIDynamicToggle Draw(Column side)
      {
        UIDynamicToggle toggle = script.CreateToggle(storable, side == Column.RIGHT);
        return toggle;
      }
    }

    // ================ CreateStringChooser ================ //
    // Create default VaM string chooser
    public static VaMStringChooser CreateStringChooser(string label, List<string> initialChoices = null, string defaultValue = null, bool filterable = false, UnityAction<string> callback = null, UnityAction callbackNoVal = null, bool register = false)
    {
      if (initialChoices == null)
      {
        initialChoices = new List<string>();
      }
      defaultValue = defaultValue ?? (initialChoices.Count > 0 ? initialChoices[0] : "");
      JSONStorableStringChooser storable = new JSONStorableStringChooser(label, initialChoices, defaultValue, label);
      if (register)
      {
        storable.storeType = JSONStorableParam.StoreType.Full;
        script.RegisterStringChooser(storable);
      }
      if (callback != null || callbackNoVal != null)
      {
        storable.setCallbackFunction = (string val) => { callback?.Invoke(val); callbackNoVal?.Invoke(); };
      }
      return new VaMStringChooser() { storable = storable, filterable = filterable };
    }

    public static VaMStringChooser CreateStringChooserKeyVal(string label, List<KeyValuePair<string, string>> initialKeyValues = null, string defaultValue = null, bool filterable = false, UnityAction<string> callback = null, UnityAction callbackNoVal = null, bool register = false)
    {
      if (initialKeyValues == null)
      {
        initialKeyValues = new List<KeyValuePair<string, string>>();
      }
      List<string> initialChoices = new List<string>();
      List<string> initialDisplayChoices = new List<string>();
      foreach (KeyValuePair<string, string> entry in initialKeyValues)
      {
        initialChoices.Add(entry.Key);
        initialDisplayChoices.Add(entry.Value);
      }
      defaultValue = defaultValue ?? (initialChoices.Count > 0 ? initialChoices[0] : "");
      JSONStorableStringChooser storable = new JSONStorableStringChooser(label, initialChoices, initialDisplayChoices, defaultValue, label);
      if (register)
      {
        storable.storeType = JSONStorableParam.StoreType.Full;
        script.RegisterStringChooser(storable);
      }
      if (callback != null || callbackNoVal != null)
      {
        storable.setCallbackFunction = (string val) => { callback?.Invoke(val); callbackNoVal?.Invoke(); };
      }
      return new VaMStringChooser() { storable = storable, filterable = filterable };
    }

    public class VaMStringChooser
    {
      public JSONStorableStringChooser storable;
      public string val { get { return storable.val; } set { storable.val = value; } }
      public string valNoCallback { set { storable.valNoCallback = value; } }
      public List<string> choices { get { return storable.choices; } set { storable.choices = value; } }
      public List<string> displayChoices { get { return storable.displayChoices; } set { storable.displayChoices = value; } }
      public bool filterable;
      public UIDynamicPopup Draw(Column side)
      {
        UIDynamicPopup popup;
        if (filterable)
        {
          popup = script.CreateFilterablePopup(storable, side == Column.RIGHT);
        }
        else
        {
          popup = script.CreateScrollablePopup(storable, side == Column.RIGHT);
        }
        return popup;
      }
    }

    // ================ CreateSlider ================ //
    // Create a custom slider with less sucky behavior
    public static VaMSlider CreateSlider(string label, float defaultValue, float defaultRange, bool allowNegative = false, bool fixedRange = false, bool exponentialRangeIncrement = false, bool integer = false, bool interactable = true, UnityAction<float> callback = null, UnityAction callbackNoVal = null, bool register = false)
    {
      float defaultMin = allowNegative ? -defaultRange : 0f;
      float defaultMax = defaultRange;
      return CreateSlider(label, defaultValue, defaultMin, defaultMax, fixedRange, exponentialRangeIncrement, integer, interactable, callback, callbackNoVal, register);
    }

    public static VaMSlider CreateSlider(string label, float defaultValue, float defaultMin, float defaultMax, bool fixedRange = false, bool exponentialRangeIncrement = false, bool integer = false, bool interactable = true, UnityAction<float> callback = null, UnityAction callbackNoVal = null, bool register = false)
    {
      JSONStorableFloat storable = new JSONStorableFloat(label, defaultValue, defaultMin, defaultMax, true, interactable);
      if (register)
      {
        storable.storeType = JSONStorableParam.StoreType.Full;
        script.RegisterFloat(storable);
      }
      if (callback != null || callbackNoVal != null)
      {
        storable.setCallbackFunction = (float val) => { callback?.Invoke(val); callbackNoVal?.Invoke(); };
      }
      return new VaMSlider()
      {
        storable = storable,
        defaultValue = defaultValue,
        defaultMin = defaultMin,
        defaultMax = defaultMax,
        fixedRange = fixedRange,
        exponentialRangeIncrement = exponentialRangeIncrement,
        integer = integer,
        interactable = interactable,
      };
    }

    private static GameObject customSliderPrefab;
    public class VaMSlider
    {
      public JSONStorableFloat storable;
      public float val { get { return storable.val; } set { storable.val = value; } }
      public float valNoCallback { set { storable.valNoCallback = value; } }
      public float min { get { return storable.min; } set { storable.min = value; } }
      public float max { get { return storable.max; } set { storable.max = value; } }
      public float defaultValue { get { return storable.defaultVal; } set { storable.defaultVal = value; } }
      public float defaultMin;
      public float defaultMax;
      public bool fixedRange;
      public bool exponentialRangeIncrement;
      public bool integer;
      public bool interactable;

      public UIDynamicCustomSlider Draw(Column side)
      {
        if (customSliderPrefab == null)
        {
          Transform basePrefab = script.manager.configurableSliderPrefab;
          customSliderPrefab = UnityEngine.Object.Instantiate(basePrefab.gameObject);
          customSliderPrefab.name = "Slider";

          UnityEngine.Object.DestroyImmediate(customSliderPrefab.GetComponent<UIDynamicSlider>());
          UnityEngine.Object.DestroyImmediate(customSliderPrefab.transform.Find("RangePanel").gameObject);

          Slider slider = customSliderPrefab.transform.Find("Slider").GetComponent<UnityEngine.UI.Slider>();
          Text sliderLabel = customSliderPrefab.transform.Find("Text").GetComponent<Text>();
          RectTransform sliderRect = customSliderPrefab.transform.Find("Slider") as RectTransform;
          sliderRect.offsetMax = new Vector2(-10f, 70f);

          SetTextFromFloat textFormatter = customSliderPrefab.transform.Find("ValueInputField").GetComponent<SetTextFromFloat>();

          Button defaultButton = customSliderPrefab.transform.Find("DefaultValueButton").GetComponent<Button>();

          Button m1Button = customSliderPrefab.transform.Find("QuickButtonsGroup/QuickButtonsLeft/M1Button").GetComponent<Button>();
          Text m1ButtonText = m1Button.transform.Find("Text").GetComponent<Text>();
          m1ButtonText.text = "-";

          Button m2Button = customSliderPrefab.transform.Find("QuickButtonsGroup/QuickButtonsLeft/M2Button").GetComponent<Button>();
          Text m2ButtonText = m2Button.transform.Find("Text").GetComponent<Text>();
          m2ButtonText.text = "--";

          Button m3Button = customSliderPrefab.transform.Find("QuickButtonsGroup/QuickButtonsLeft/M3Button").GetComponent<Button>();
          Text m3ButtonText = m3Button.transform.Find("Text").GetComponent<Text>();
          m3ButtonText.text = "---";

          Button mRangeButton = customSliderPrefab.transform.Find("QuickButtonsGroup/QuickButtonsLeft/M4Button").GetComponent<Button>();
          Text mRangeButtonText = mRangeButton.transform.Find("Text").GetComponent<Text>();
          mRangeButton.gameObject.name = "MRangeButton";
          mRangeButtonText.text = "- Rng";

          Button p1Button = customSliderPrefab.transform.Find("QuickButtonsGroup/QuickButtonsRight/P1Button").GetComponent<Button>();
          Text p1ButtonText = p1Button.transform.Find("Text").GetComponent<Text>();
          p1ButtonText.text = "+";

          Button p2Button = customSliderPrefab.transform.Find("QuickButtonsGroup/QuickButtonsRight/P2Button").GetComponent<Button>();
          Text p2ButtonText = p2Button.transform.Find("Text").GetComponent<Text>();
          p2ButtonText.text = "++";

          Button p3Button = customSliderPrefab.transform.Find("QuickButtonsGroup/QuickButtonsRight/P3Button").GetComponent<Button>();
          Text p3ButtonText = p3Button.transform.Find("Text").GetComponent<Text>();
          p3ButtonText.text = "+++";

          Button pRangeButton = customSliderPrefab.transform.Find("QuickButtonsGroup/QuickButtonsRight/P4Button").GetComponent<Button>();
          Text pRangeButtonText = pRangeButton.transform.Find("Text").GetComponent<Text>();
          pRangeButtonText.gameObject.name = "PRangeButton";
          pRangeButtonText.text = "+ Rng";

          UIDynamicCustomSlider uid = customSliderPrefab.AddComponent<UIDynamicCustomSlider>();
          uid.slider = slider;
          uid.label = sliderLabel;
          uid.textFormatter = textFormatter;
          uid.m1ButtonText = m1ButtonText;
          uid.m2ButtonText = m2ButtonText;
          uid.m3ButtonText = m3ButtonText;
          uid.p1ButtonText = p1ButtonText;
          uid.p2ButtonText = p2ButtonText;
          uid.p3ButtonText = p3ButtonText;
          uid.mRangeButtonText = mRangeButtonText;
          uid.pRangeButtonText = pRangeButtonText;
          uid.defaultButton = defaultButton;
          uid.m1Button = m1Button;
          uid.m2Button = m2Button;
          uid.m3Button = m3Button;
          uid.p1Button = p1Button;
          uid.p2Button = p2Button;
          uid.p3Button = p3Button;
          uid.mRangeButton = mRangeButton;
          uid.pRangeButton = pRangeButton;
        }
        {
          Transform t = createUIElement(customSliderPrefab.transform, side == Column.RIGHT);
          UIDynamicCustomSlider uid = t.GetComponent<UIDynamicCustomSlider>();
          storable.RegisterSlider(uid.slider);
          uid.storable = storable;
          uid.fixedRange = fixedRange;
          uid.exponentialRangeIncrement = exponentialRangeIncrement;
          uid.integer = integer;
          uid.interactable = interactable;
          uid.defaultValue = defaultValue;
          uid.defaultMin = defaultMin;
          uid.defaultMax = defaultMax;
          uid.gameObject.SetActive(true);
          return uid;
        }
      }
    }

    // ================ CreateTextInput ================ //
    // Create one-line text input with a label
    public static VaMTextInput CreateTextInput(string label, string defaultValue = "", UnityAction<string> callback = null, UnityAction callbackNoVal = null, bool register = false)
    {
      JSONStorableString storable = new JSONStorableString(label, defaultValue);
      if (register)
      {
        storable.storeType = JSONStorableParam.StoreType.Full;
        script.RegisterString(storable);
      }
      if (callback != null || callbackNoVal != null)
      {
        storable.setCallbackFunction = (string val) => { callback?.Invoke(val); callbackNoVal?.Invoke(); };
      }
      return new VaMTextInput() { storable = storable };
    }

    private static GameObject customOnelineTextInputPrefab;
    public class VaMTextInput
    {
      public JSONStorableString storable;
      public string val { get { return storable.val; } set { storable.val = value; } }
      public string valNoCallback { set { storable.valNoCallback = value; } }
      public UIDynamicOnelineTextInput Draw(Column side)
      {
        if (customOnelineTextInputPrefab == null)
        {
          UIDynamicOnelineTextInput uid = CreateUIDynamicPrefab<UIDynamicOnelineTextInput>("TextInput", 50f);
          customOnelineTextInputPrefab = uid.gameObject;

          RectTransform background = InstantiateBackground(uid.transform);

          RectTransform labelRect = InstantiateLabel(uid.transform);
          labelRect.anchorMax = new Vector2(0.4f, 1f);
          Text labelText = labelRect.GetComponent<Text>();
          labelText.text = "";
          labelText.color = Color.white;

          RectTransform inputRect = InstantiateTextField(uid.transform);
          inputRect.anchorMin = new Vector2(0.4f, 0f);
          inputRect.offsetMin = new Vector2(5f, 5f);
          inputRect.offsetMax = new Vector2(-5f, -4f);

          RectTransform textRect = inputRect.Find("Scroll View/Viewport/Content/Text") as RectTransform;
          textRect.offsetMin = new Vector2(10f, 0f);
          textRect.offsetMax = new Vector2(-10f, -5f);

          uid.label = labelText;
          uid.input = inputRect.GetComponent<InputField>();
        }
        {
          string label = storable.name;
          Transform t = createUIElement(customOnelineTextInputPrefab.transform, side == Column.RIGHT);
          UIDynamicOnelineTextInput uid = t.GetComponent<UIDynamicOnelineTextInput>();
          uid.label.text = label;
          storable.inputField = uid.input;
          uid.gameObject.SetActive(true);
          return uid;
        }
      }
    }

    // ================ CreateLabelWithToggle ================ //
    // Create label that has a toggle on the right side
    // Not much different than a normal toggle -- but a good example of how to do a custom toggle
    public static VaMLabelWithToggle CreateLabelWithToggle(string label, bool defaultValue, UnityAction<bool> callback = null, UnityAction callbackNoVal = null, bool register = false)
    {
      JSONStorableBool storable = new JSONStorableBool(label, defaultValue);
      if (register)
      {
        storable.storeType = JSONStorableParam.StoreType.Full;
        script.RegisterBool(storable);
      }
      if (callback != null || callbackNoVal != null)
      {
        storable.setCallbackFunction = (bool val) => { callback?.Invoke(val); callbackNoVal?.Invoke(); };
      }
      return new VaMLabelWithToggle() { storable = storable };
    }

    private static GameObject customLabelWithTogglePrefab;
    public class VaMLabelWithToggle
    {
      public JSONStorableBool storable;
      public bool val { get { return storable.val; } set { storable.val = value; } }
      public bool valNoCallback { set { storable.valNoCallback = value; } }
      public UIDynamicLabelWithToggle Draw(Column side)
      {
        if (customLabelWithTogglePrefab == null)
        {
          UIDynamicLabelWithToggle uid = CreateUIDynamicPrefab<UIDynamicLabelWithToggle>("LabelWithToggle", 50f);
          customLabelWithTogglePrefab = uid.gameObject;

          RectTransform background = InstantiateBackground(uid.transform);
          Image bgImage = background.GetComponent<Image>();
          bgImage.color = new Color(1f, 1f, 1f, 0.35f);

          RectTransform labelRect = InstantiateLabel(uid.transform);
          Text labelText = labelRect.GetComponent<Text>();
          labelText.text = "";

          RectTransform toggleRect = InstantiateToggle(uid.transform, 45f);
          toggleRect.anchorMin = new Vector2(1f, 0f);
          toggleRect.offsetMin = new Vector2(-45f, 0f);
          toggleRect.offsetMax = new Vector2(0f, -2.5f);

          uid.label = labelText;
          uid.toggle = toggleRect.GetComponent<Toggle>();
        }
        {
          string label = storable.name;
          Transform t = createUIElement(customLabelWithTogglePrefab.transform, side == Column.RIGHT);
          UIDynamicLabelWithToggle uid = t.GetComponent<UIDynamicLabelWithToggle>();
          uid.label.text = label;
          storable.RegisterToggle(uid.toggle);
          uid.gameObject.SetActive(true);
          return uid;
        }
      }
    }

    // ================ CreateColorPicker ================ //
    // Create default VaM color picker
    public static VaMColorPicker CreateColorPicker(string label, HSVColor defaultValue, UnityAction<HSVColor> callback = null, UnityAction callbackNoVal = null, bool register = false)
    {
      JSONStorableColor storable = new JSONStorableColor(label, defaultValue);
      if (register)
      {
        storable.storeType = JSONStorableParam.StoreType.Full;
        script.RegisterColor(storable);
      }
      if (callback != null || callbackNoVal != null)
      {
        storable.setCallbackFunction = (float h, float s, float v) => { callback?.Invoke(new HSVColor() { H = h, S = s, V = v }); callbackNoVal?.Invoke(); };
      }
      return new VaMColorPicker() { storable = storable };
    }

    public class VaMColorPicker
    {
      public JSONStorableColor storable;
      public HSVColor val { get { return storable.val; } set { storable.val = value; } }
      public HSVColor valNoCallback { set { storable.valNoCallback = value; } }
      public UIDynamicColorPicker Draw(Column side)
      {
        UIDynamicColorPicker picker = script.CreateColorPicker(storable, side == Column.RIGHT);
        return picker;
      }
    }

    // ================ CreateFileSelect ================ //
    // Create a button for choosing a file
    public static VaMFileSelect CreateFileSelect(string label, string fileExtension = "", string path = "", bool browsePathOnly = false, bool browseFullComputer = false, bool clearStorableAfterSelect = true, UnityAction<string> callback = null, UnityAction callbackNoVal = null, bool register = false, Color? buttonColor = null)
    {
      JSONStorableUrl storable = new JSONStorableUrl(label, "", fileExtension, path);
      if (register)
      {
        storable.storeType = JSONStorableParam.StoreType.Full;
        script.RegisterUrl(storable);
      }
      if (callback != null || callbackNoVal != null)
      {
        storable.setCallbackFunction = (string val) =>
        {
          callback?.Invoke(val);
          callbackNoVal?.Invoke();
          if (clearStorableAfterSelect)
          {
            storable.valNoCallback = storable.defaultVal;
          }
        };
      }
      storable.allowBrowseAboveSuggestedPath = !browsePathOnly;
      storable.allowFullComputerBrowse = browseFullComputer;
      return new VaMFileSelect() { storable = storable, buttonColor = buttonColor };
    }

    public class VaMFileSelect
    {
      public JSONStorableUrl storable;
      public string val { get { return storable.val; } set { storable.val = value; } }
      public string valNoCallback { set { storable.valNoCallback = value; } }
      public Color? buttonColor;
      public UIDynamicButton Draw(Column side)
      {
        UIDynamicButton button = CreateButton(side, storable.name);
        if (buttonColor != null)
        {
          button.buttonColor = buttonColor.Value;
        }
        storable.RegisterFileBrowseButton(button.button);
        return button;
      }
    }

    // ================ CreateFileSave ================ //
    // Create a button for saving a file
    public static UIDynamicButton CreateFileSave(Column side, string label, string defaultFilename = null, string fileExtension = "", string path = "", UnityAction<string> callback = null, Color? buttonColor = null)
    {
      UIDynamicButton button = CreateButton(side, label, () =>
      {
        SuperController sc = SuperController.singleton;
        sc.GetMediaPathDialog(
          (string val) => { callback?.Invoke(val); },
          filter: fileExtension,
          suggestedFolder: path,
          fullComputerBrowse: false,
          showDirs: true,
          showKeepOpt: false,
          fileRemovePrefix: null,
          hideExtenstion: false,
          shortCuts: null,
          browseVarFilesAsDirectories: false,
          showInstallFolderInDirectoryList: false
        );
        sc.mediaFileBrowserUI.SetTextEntry(true);
        if (sc.mediaFileBrowserUI.fileEntryField != null)
        {
          sc.mediaFileBrowserUI.fileEntryField.text = defaultFilename ?? $"{VaMUtils.GetTimestamp()}.{fileExtension}";
          sc.mediaFileBrowserUI.ActivateFileNameField();
        }
      });
      if (buttonColor != null)
      {
        button.buttonColor = buttonColor.Value;
      }
      return button;
    }

    // ================ RemoveUIElements ================ //
    // Call to remove a list of UI elements before rebuilding your UI.
    public static void RemoveUIElements(ref List<object> menuElements)
    {
      for (int i = 0; i < menuElements.Count; ++i)
      {
        if (menuElements[i] is JSONStorableParam)
        {
          JSONStorableParam jsp = menuElements[i] as JSONStorableParam;
          if (jsp is JSONStorableFloat)
            script.RemoveSlider(jsp as JSONStorableFloat);
          else if (jsp is JSONStorableBool)
            script.RemoveToggle(jsp as JSONStorableBool);
          else if (jsp is JSONStorableColor)
            script.RemoveColorPicker(jsp as JSONStorableColor);
          else if (jsp is JSONStorableString)
            script.RemoveTextField(jsp as JSONStorableString);
          else if (jsp is JSONStorableStringChooser)
          {
            // Workaround for VaM not cleaning its panels properly.
            JSONStorableStringChooser jssc = jsp as JSONStorableStringChooser;
            RectTransform popupPanel = jssc.popup?.popupPanel;
            script.RemovePopup(jssc);
            if (popupPanel != null)
              UnityEngine.Object.Destroy(popupPanel.gameObject);
          }
        }
        else if (menuElements[i] is UIDynamic)
        {
          UIDynamic uid = menuElements[i] as UIDynamic;
          if (uid is UIDynamicButton)
            script.RemoveButton(uid as UIDynamicButton);
          else if (uid is UIDynamicBase)
            script.RemoveSpacer(uid);
          else if (uid is UIDynamicSlider)
            script.RemoveSlider(uid as UIDynamicSlider);
          else if (uid is UIDynamicToggle)
            script.RemoveToggle(uid as UIDynamicToggle);
          else if (uid is UIDynamicColorPicker)
            script.RemoveColorPicker(uid as UIDynamicColorPicker);
          else if (uid is UIDynamicTextField)
            script.RemoveTextField(uid as UIDynamicTextField);
          else if (uid is UIDynamicPopup)
          {
            // Workaround for VaM not cleaning its panels properly.
            UIDynamicPopup uidp = uid as UIDynamicPopup;
            RectTransform popupPanel = uidp.popup?.popupPanel;
            script.RemovePopup(uidp);
            if (popupPanel != null)
              UnityEngine.Object.Destroy(popupPanel.gameObject);
          }
          else
            script.RemoveSpacer(uid);
        }
      }
      menuElements.Clear();
    }
  }


  // ======================================================================================================= //
  // ========================================== CUSTOM UI HELPERS ========================================== //
  // ======================================================================================================= //
  // These methods instantiate basic UI elements to make building custom components easier
  // See custom UI methods above for examples
  public static partial class VaMUI
  {
    // Creates a basic object with a RectTransform and a LayoutElement for building UI prefabs
    public static T CreateUIDynamicPrefab<T>(string name, float height = 50f) where T : UIDynamicBase
    {
      GameObject obj = new GameObject(name);
      obj.SetActive(false);
      RectTransform rectTransform = obj.AddComponent<RectTransform>();
      ResetRectTransform(rectTransform);
      LayoutElement layout = obj.AddComponent<LayoutElement>();
      ResetLayoutElement(layout, height);
      T uid = obj.AddComponent<T>();
      uid.rectTransform = rectTransform;
      uid.layout = layout;
      return uid;
    }

    // Creates an empty rect element
    private static GameObject rectElementPrefab;
    public static RectTransform InstantiateEmptyRect(Transform parent)
    {
      if (rectElementPrefab == null)
      {
        rectElementPrefab = UnityEngine.Object.Instantiate(new GameObject(""));
        rectElementPrefab.name = "Container";
        RectTransform rect = rectElementPrefab.AddComponent<RectTransform>();
        ResetRectTransform(rect);
      }
      return VaMUtils.InstantiateWithSameName(rectElementPrefab.transform as RectTransform, parent);
    }

    // Creates a semi-opaque background element
    private static GameObject backgroundElementPrefab;
    public static RectTransform InstantiateBackground(Transform parent)
    {
      if (backgroundElementPrefab == null)
      {
        Transform basePrefab = script.manager.configurableScrollablePopupPrefab.transform.Find("Background");
        backgroundElementPrefab = UnityEngine.Object.Instantiate(basePrefab.gameObject);
        backgroundElementPrefab.name = "Background";
        ResetRectTransform(backgroundElementPrefab.transform as RectTransform);
      }
      return VaMUtils.InstantiateWithSameName(backgroundElementPrefab.transform as RectTransform, parent);
    }

    // Creates a label element
    private static GameObject labelElementPrefab;
    public static RectTransform InstantiateLabel(Transform parent)
    {
      if (labelElementPrefab == null)
      {
        Transform basePrefab = script.manager.configurableScrollablePopupPrefab.transform.Find("Button/Text");
        labelElementPrefab = UnityEngine.Object.Instantiate(basePrefab.gameObject);
        labelElementPrefab.name = "Label";
        ResetRectTransform(labelElementPrefab.transform as RectTransform);
      }
      return VaMUtils.InstantiateWithSameName(labelElementPrefab.transform as RectTransform, parent);
    }

    // Creates a text input element
    private static GameObject textFieldElementPrefab;
    public static RectTransform InstantiateTextField(Transform parent)
    {
      if (textFieldElementPrefab == null)
      {
        Transform basePrefab = script.manager.configurableTextFieldPrefab.transform as RectTransform;
        textFieldElementPrefab = UnityEngine.Object.Instantiate(basePrefab.gameObject);
        textFieldElementPrefab.name = "TextField";
        ResetRectTransform(textFieldElementPrefab.transform as RectTransform);
        UnityEngine.Object.DestroyImmediate(textFieldElementPrefab.GetComponent<LayoutElement>());

        UIDynamicTextField textField = textFieldElementPrefab.GetComponent<UIDynamicTextField>();
        textField.backgroundColor = Color.white;
        InputField inputfield = textFieldElementPrefab.gameObject.AddComponent<InputField>();
        inputfield.textComponent = textField.UItext;

        RectTransform textRect = textFieldElementPrefab.transform.Find("Scroll View/Viewport/Content/Text") as RectTransform;
        textRect.offsetMin = new Vector2(10f, 0f);
        textRect.offsetMax = new Vector2(-10f, -5f);
      }
      return VaMUtils.InstantiateWithSameName(textFieldElementPrefab.transform as RectTransform, parent);
    }

    // Creates a button element
    private static GameObject buttonElementPrefab;
    public static RectTransform InstantiateButton(Transform parent)
    {
      if (buttonElementPrefab == null)
      {
        Transform basePrefab = script.manager.configurableButtonPrefab;
        buttonElementPrefab = UnityEngine.Object.Instantiate(basePrefab.gameObject);
        buttonElementPrefab.name = "Button";
        ResetRectTransform(buttonElementPrefab.transform as RectTransform);
      }
      return VaMUtils.InstantiateWithSameName(buttonElementPrefab.transform as RectTransform, parent);
    }

    // Creates a toggle element
    private static GameObject toggleElementPrefab;
    public static RectTransform InstantiateToggle(Transform parent, float size = 50f)
    {
      if (toggleElementPrefab == null)
      {
        Transform basePrefab = script.manager.configurableTogglePrefab;
        toggleElementPrefab = UnityEngine.Object.Instantiate(basePrefab.gameObject);
        toggleElementPrefab.name = "Toggle";

        UnityEngine.Object.DestroyImmediate(toggleElementPrefab.GetComponent<LayoutElement>());
        UnityEngine.Object.DestroyImmediate(toggleElementPrefab.GetComponent<UIDynamicToggle>());
        UnityEngine.Object.DestroyImmediate(toggleElementPrefab.transform.Find("Panel").gameObject);
        UnityEngine.Object.DestroyImmediate(toggleElementPrefab.transform.Find("Label").gameObject);

        RectTransform rect = toggleElementPrefab.transform as RectTransform;
        RectTransform backgroundRect = toggleElementPrefab.transform.Find("Background") as RectTransform;
        RectTransform checkmarkRect = toggleElementPrefab.transform.Find("Background/Checkmark") as RectTransform;

        ResetRectTransform(rect);
        ResetRectTransform(backgroundRect);
        ResetRectTransform(checkmarkRect);
        backgroundRect.anchorMin = new Vector2(0f, 1f);
        backgroundRect.anchorMax = new Vector2(0f, 1f);
        backgroundRect.offsetMin = new Vector2(0f, -50f);
        backgroundRect.offsetMax = new Vector2(50f, 0f);
      }
      {
        RectTransform toggle = VaMUtils.InstantiateWithSameName(toggleElementPrefab.transform as RectTransform, parent);
        RectTransform backgroundRect = toggle.Find("Background") as RectTransform;
        backgroundRect.offsetMin = new Vector2(0f, -size);
        backgroundRect.offsetMax = new Vector2(size, 0f);
        return toggle;
      }
    }

    // Reset a RectTransform to useful defaults
    public static void ResetRectTransform(RectTransform transform)
    {
      transform.anchoredPosition = new Vector2(0f, 0f);
      transform.anchorMin = new Vector2(0f, 0f);
      transform.anchorMax = new Vector2(1f, 1f);
      transform.offsetMin = new Vector2(0f, 0f);
      transform.offsetMax = new Vector2(0f, 0f);
      transform.pivot = new Vector2(0.5f, 0.5f);
    }

    // Reset a LayoutElement to useful defaults
    public static void ResetLayoutElement(LayoutElement layout, float height = 0f, bool flexWidth = true)
    {
      layout.flexibleWidth = flexWidth ? 1f : -1f;
      layout.flexibleHeight = -1f;
      layout.minWidth = -1f;
      layout.minHeight = height;
      layout.preferredWidth = -1f;
      layout.preferredHeight = height;
    }
  }


  // =================================================================================================== //
  // ========================================== TRIGGER UTILS ========================================== //
  // =================================================================================================== //
  // Usage:
  // - In script Init(), call:
  //       VaMUI.InitTriggerUtils(this);
  // - In script OnDestroy(), call:
  //       VaMUI.DestroyTriggerUtils();
  //
  // Credit to AcidBubbles for figuring out how to do custom triggers.
  public static partial class VaMUI
  {
    public static bool triggerUtilsInitialized { get; private set; }
    public static bool triggerPrefabsLoaded { get; private set; }
    public static CustomTriggerHandler triggerHandler { get; private set; }

    private static GameObject customTriggerActionsPrefab;
    private static GameObject customTriggerActionMiniPrefab;
    private static GameObject customTriggerActionDiscretePrefab;
    private static GameObject customTriggerActionTransitionPrefab;

    public static void InitTriggerUtils(MVRScript script)
    {
      VaMUI.script = script;
      VaMUI.triggerHandler = script.gameObject.AddComponent<CustomTriggerHandler>();
      SuperController.singleton.StartCoroutine(LoadAssets());
      VaMUI.triggerUtilsInitialized = true;
    }

    public static void DestroyTriggerUtils()
    {
      VaMUtils.SafeDestroy(ref customTriggerActionsPrefab);
      VaMUtils.SafeDestroy(ref customTriggerActionMiniPrefab);
      VaMUtils.SafeDestroy(ref customTriggerActionDiscretePrefab);
      VaMUtils.SafeDestroy(ref customTriggerActionTransitionPrefab);
      if (triggerHandler != null)
      {
        UnityEngine.Object.Destroy(triggerHandler);
      }
    }

    // ================ CreateEventTrigger ================ //
    // Create an EventTrigger
    public static EventTrigger CreateEventTrigger(string name)
    {
      return new EventTrigger(name);
    }

    // ================ CreateValueTrigger ================ //
    // Create a ValueTrigger
    public static ValueTrigger CreateValueTrigger(string name)
    {
      return new ValueTrigger(name);
    }

    private static IEnumerator LoadAssets()
    {
      foreach (var x in LoadAsset("z_ui2", "TriggerActionsPanel", CreateTriggerActionsPrefab))
        yield return x;
      foreach (var x in LoadAsset("z_ui2", "TriggerActionMiniPanel", CreateTriggerActionMiniPrefab))
        yield return x;
      foreach (var x in LoadAsset("z_ui2", "TriggerActionDiscretePanel", CreateTriggerActionDiscretePrefab))
        yield return x;
      foreach (var x in LoadAsset("z_ui2", "TriggerActionTransitionPanel", CreateTriggerActionTransitionPrefab))
        yield return x;

      triggerPrefabsLoaded = true;
    }

    private static IEnumerable LoadAsset(string assetBundleName, string assetName, Action<GameObject> assign)
    {
      AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(assetBundleName, assetName, typeof(GameObject));
      if (request == null) throw new NullReferenceException($"Request for {assetName} in {assetBundleName} assetbundle failed: Null request.");
      yield return request;
      GameObject prefab = request.GetAsset<GameObject>();
      if (prefab == null) throw new NullReferenceException($"Request for {assetName} in {assetBundleName} assetbundle failed: Null GameObject.");
      assign(prefab);
    }

    private static void CreateTriggerActionsPrefab(GameObject basePrefab)
    {
      customTriggerActionsPrefab = GameObject.Instantiate(basePrefab);
    }

    private static void CreateTriggerActionMiniPrefab(GameObject basePrefab)
    {
      customTriggerActionMiniPrefab = GameObject.Instantiate(basePrefab);
    }

    private static void CreateTriggerActionDiscretePrefab(GameObject basePrefab)
    {
      customTriggerActionDiscretePrefab = GameObject.Instantiate(basePrefab);
    }

    private static void CreateTriggerActionTransitionPrefab(GameObject basePrefab)
    {
      customTriggerActionTransitionPrefab = GameObject.Instantiate(basePrefab);
      customTriggerActionTransitionPrefab.GetComponent<TriggerActionTransitionUI>().startWithCurrentValToggle.gameObject.SetActive(false);
    }
  }


  // =================================================================================================== //
  // ========================================== GENERAL UTILS ========================================== //
  // =================================================================================================== //
  public static class VaMUtils
  {
    // VaM Plugins can contain multiple Scripts, if you load them via a *.cslist file. This function allows you to get
    // an instance of another script within the same plugin, allowing you directly interact with it by reading/writing
    // data, calling functions, etc.
    public static T FindWithinSamePlugin<T>(MVRScript self) where T : MVRScript
    {
      int i = self.name.IndexOf('_');
      if (i < 0)
        return null;
      string prefix = self.name.Substring(0, i + 1);
      string scriptName = prefix + typeof(T).FullName;
      return self.containingAtom.GetStorableByID(scriptName) as T;
    }

    // Get spawned prefab from CustomUnityAsset atom. Note that these are loaded asynchronously,
    // this function returns null while the prefab is not yet there.
    public static GameObject GetCustomUnityAsset(Atom atom, string prefabName)
    {
      Transform t = atom.transform.Find("reParentObject/object/rescaleObject/" + prefabName + "(Clone)");
      if (t == null)
        return null;
      else
        return t.gameObject;
    }

    // Get directory path where the plugin is located. Based on Alazi's & VAMDeluxe's method.
    public static string GetPluginPath(MVRScript self)
    {
      string id = self.name.Substring(0, self.name.IndexOf('_'));
      string filename = self.manager.GetJSON()["plugins"][id].Value;
      return filename.Substring(0, filename.LastIndexOfAny(new char[] { '/', '\\' }));
    }

    // Get path prefix of the package that contains our plugin.
    public static string GetPackagePath(MVRScript self)
    {
      string id = self.name.Substring(0, self.name.IndexOf('_'));
      string filename = self.manager.GetJSON()["plugins"][id].Value;
      int idx = filename.IndexOf(":/");
      if (idx >= 0)
        return filename.Substring(0, idx + 2);
      else
        return string.Empty;
    }

    // Check if our plugin is running from inside a package
    public static bool IsInPackage(MVRScript self)
    {
      string id = self.name.Substring(0, self.name.IndexOf('_'));
      string filename = self.manager.GetJSON()["plugins"][id].Value;
      return filename.IndexOf(":/") >= 0;
    }

    // Helper to add a component if missing.
    public static T GetOrAddComponent<T>(Component c) where T : Component
    {
      T t = c.GetComponent<T>();
      if (t == null)
        t = c.gameObject.AddComponent<T>();
      return t;
    }

    // Generate a random id
    private const string IDAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-=";
    public static string GenerateRandomID(int length = 16)
    {
      StringBuilder str = new StringBuilder();

      for (int i = 0; i < length; i++)
      {
        int r = UnityEngine.Random.Range(0, IDAlphabet.Length);
        str.Append(IDAlphabet[r]);
      }

      return str.ToString();
    }

    public static string GetTimestamp()
    {
      return ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString();
    }

    // Set string chooser choices/displayChoices from a key-value list
    public static void SetStringChooserChoices(JSONStorableStringChooser storable, List<KeyValuePair<string, string>> entries)
    {
      List<string> choices = new List<string>();
      List<string> displayChoices = new List<string>();
      foreach (KeyValuePair<string, string> entry in entries)
      {
        choices.Add(entry.Key);
        displayChoices.Add(entry.Value);
      }
      storable.choices = choices;
      storable.displayChoices = displayChoices;
    }

    // Select the first value of a string chooser
    public static void SelectStringChooserFirstValue(JSONStorableStringChooser storable, bool noCallback = false)
    {
      string newVal = "";
      if (storable.choices.Count > 0)
      {
        newVal = storable.choices[0];
      }
      if (noCallback)
      {
        storable.valNoCallback = newVal;
      }
      else
      {
        storable.val = newVal;
      }
    }

    // Reset a string chooser if its current value doesn't match any of its choices
    public static void EnsureStringChooserValue(JSONStorableStringChooser storable, bool defaultToFirstChoice = false, bool noCallback = false)
    {
      if (storable.choices.Contains(storable.val))
      {
        return;
      }
      string newVal = "";
      if (storable.choices.Count > 0 && defaultToFirstChoice)
      {
        newVal = storable.choices[0];
      }
      if (noCallback)
      {
        storable.valNoCallback = newVal;
      }
      else
      {
        storable.val = newVal;
      }
    }

    public static string GetStringChooserDisplayFromVal(JSONStorableStringChooser storable, string val)
    {
      int index = storable.choices.FindIndex((v) => v == val);
      if (index < 0 || index >= storable.displayChoices.Count) return "";
      return storable.displayChoices[index];
    } 

    // Add/Subtract slider range by current power of 10, clamping within range 1 - 100000
    public static void AddSliderRange(JSONStorableFloat storable, bool invert)
    {
      float maxRange = Mathf.Max(Math.Abs(storable.max), Math.Abs(storable.min));
      float newRange;
      float pow10 = VaMMath.CurrentPowerOf10(maxRange);
      if (pow10 < 1f) newRange = 1f;
      else
      {
        int maxDigit = (int)maxRange / (int)pow10;
        if (invert)
        {
          if (maxRange > maxDigit * pow10) maxDigit++;
          if (maxDigit <= 1) newRange = 0.9f * pow10;
          else newRange = (maxDigit - 1) * pow10;
        }
        else newRange = (maxDigit + 1) * pow10;
      }
      newRange = Mathf.Clamp(newRange, 1f, 100000f);
      if (storable.min > 0f) storable.min = 0f;
      if (storable.min != 0f) storable.min = -newRange;
      storable.max = newRange;
    }

    // Multiply/divide slider range by 10, clamping to power of 10 within range 1 - 100000
    public static void MultiplySliderRange(JSONStorableFloat storable, bool invert)
    {
      float maxRange = Mathf.Max(Math.Abs(storable.max), Math.Abs(storable.min));
      float newRange = invert ? VaMMath.PrevPowerOf10(maxRange) : VaMMath.NextPowerOf10(maxRange);
      newRange = Mathf.Clamp(newRange, 1f, 100000f);
      if (storable.min > 0f) storable.min = 0f;
      if (storable.min != 0f) storable.min = -newRange;
      storable.max = newRange;
    }

    // Destroy a potentially null game object safely
    public static void SafeDestroy(ref GameObject obj)
    {
      if (obj != null)
      {
        UnityEngine.Object.Destroy(obj);
        obj = null;
      }
    }

    // Instantiate a game object but without the (Clone) affix
    public static T InstantiateWithSameName<T>(T original, Transform parent) where T : UnityEngine.Object
    {
      T obj = UnityEngine.Object.Instantiate(original, parent);
      obj.name = original.name;
      return obj;
    }

    // Recursively log a game object and its children (can explode if the log gets too big)
    public static void LogGameObjectTree(GameObject obj, int maxDepth = 99, bool logComponents = true, bool logProps = false)
    {
      StringBuilder str = new StringBuilder();
      LogGameObjectTreeInternal(obj, 0, str, maxDepth, logComponents, logProps);
      str.Append("\n");
      SuperController.LogMessage(str.ToString());
    }
    public static void LogGameObjectTree(Transform obj, int maxDepth = 99, bool logComponents = true, bool logProps = false)
    {
      LogGameObjectTree(obj.gameObject, maxDepth, logComponents, logProps);
    }

    private static void LogGameObjectTreeInternal(GameObject obj, int depth, StringBuilder str, int maxDepth, bool logComponents, bool logProps)
    {
      if (depth >= maxDepth) return;
      LogGameObjectTreeChildLineStart(depth, str);
      str.Append(obj.name).Append("\n");

      if (logComponents)
      {
        Component[] components = obj.GetComponents<Component>();
        foreach (Component component in components)
        {
          LogGameObjectTreeComponentLineStart(depth, str);
          str.Append(component.GetType().ToString()).Append("\n");

          if (!logProps) continue;

          if (component is RectTransform)
          {
            RectTransform rt = component as RectTransform;
            LogGameObjectTreeProp(depth, str, "anchoredPosition", rt.anchoredPosition);
            LogGameObjectTreeProp(depth, str, "anchorMin", rt.anchorMin);
            LogGameObjectTreeProp(depth, str, "anchorMax", rt.anchorMax);
            LogGameObjectTreeProp(depth, str, "offsetMin", rt.offsetMin);
            LogGameObjectTreeProp(depth, str, "offsetMax", rt.offsetMax);
            LogGameObjectTreeProp(depth, str, "pivot", rt.pivot);
            LogGameObjectTreeProp(depth, str, "rect", rt.rect);
          }
          else if (component is LayoutElement)
          {
            LayoutElement le = component as LayoutElement;
            LogGameObjectTreeProp(depth, str, "flexibleWidth", le.flexibleWidth);
            LogGameObjectTreeProp(depth, str, "flexibleHeight", le.flexibleHeight);
            LogGameObjectTreeProp(depth, str, "minWidth", le.minWidth);
            LogGameObjectTreeProp(depth, str, "minHeight", le.minHeight);
            LogGameObjectTreeProp(depth, str, "preferredWidth", le.preferredWidth);
            LogGameObjectTreeProp(depth, str, "preferredHeight", le.preferredHeight);
            LogGameObjectTreeProp(depth, str, "ignoreLayout", le.ignoreLayout);
            LogGameObjectTreeProp(depth, str, "layoutPriority", le.layoutPriority);
          }
          else if (component is Image)
          {
            Image img = component as Image;
            LogGameObjectTreeProp(depth, str, "mainTexture", img.mainTexture?.name);
            LogGameObjectTreeProp(depth, str, "sprite", img.sprite?.name);
            LogGameObjectTreeProp(depth, str, "color", img.color);
          }
        }
      }

      for (int i = 0; i < obj.transform.childCount; i++)
      {
        Transform child = obj.transform.GetChild(i);
        LogGameObjectTreeInternal(child.gameObject, depth + 1, str, maxDepth, logComponents, logProps);
      }
    }

    private static void LogGameObjectTreeChildLineStart(int depth, StringBuilder str)
    {
      for (int i = 0; i < depth - 1; i++) { str.Append("│   "); }
      if (depth > 0) { str.Append("├─"); }
      str.Append("┬");
    }

    private static void LogGameObjectTreeComponentLineStart(int depth, StringBuilder str)
    {
      for (int i = 0; i < depth; i++) { str.Append("│   "); }
      str.Append("│   ");
    }

    private static void LogGameObjectTreeProp(int depth, StringBuilder str, string name, object val)
    {
      LogGameObjectTreeComponentLineStart(depth, str);
      str.Append("    ").Append(name).Append("=").Append(val.ToString()).Append("\n");
    }
  }


  // ================================================================================================ //
  // ========================================== MATH UTILS ========================================== //
  // ================================================================================================ //
  public static class VaMMath
  {
    // Get next power of 10 strictly larger than n
    public static float NextPowerOf10(float n)
    {
      if (n <= 0f) return 0;
      float p = Mathf.Log10(n);
      if (p % 1f == 0f)
      {
        p += 1f;
      }
      return Mathf.Pow(10, Mathf.Ceil(p));
    }

    // Get next power of 10 strictly smaller than n
    public static float PrevPowerOf10(float n)
    {
      if (n <= 0f) return 0;
      float p = Mathf.Log10(n);
      if (p % 1f == 0f)
      {
        p -= 1f;
      }
      return Mathf.Pow(10, Mathf.Floor(p));
    }

    // Get magnitude of current number
    public static float CurrentPowerOf10(float n)
    {
      if (n <= 0f) return 0;
      float p = Mathf.Log10(n);
      return Mathf.Pow(10f, Mathf.Floor(p));
    }
  }


  // ========================================================================================================= //
  // ========================================== MISC HELPER CLASSES ========================================== //
  // ========================================================================================================= //
  // You shouldn't need to mess with these at all

  // delegates
  public delegate Transform CreateUIElement(Transform prefab, bool rightSide);

  // dumb custom enum stuff since VaM explodes if you make an enum
  public static partial class VaMUI
  {
    public struct Column : IEquatable<Column>
    {
      public readonly static Column LEFT = new Column(0);
      public readonly static Column RIGHT = new Column(1);

      private int value;
      private Column(int value)
      {
        this.value = value;
      }

      public bool Equals(Column other)
      {
        return other.value == this.value;
      }

      public override bool Equals(object obj)
      {
        if (obj is Column)
        {
          return this.Equals((Column)obj);
        }
        return false;
      }

      public override int GetHashCode()
      {
        return value;
      }

      public static bool operator ==(Column a, Column b)
      {
        return a.Equals(b);
      }

      public static bool operator !=(Column a, Column b)
      {
        return !a.Equals(b);
      }
    }
  }

  // helper classes for making triggers work with minimal manual management
  public static partial class VaMUI
  {
    // handler monobehaviour gets attatched to MVRScript gameobject
    public class CustomTriggerHandler : MonoBehaviour, TriggerHandler
    {
      List<TriggerActionDiscrete> actionsNeedingUpdate = new List<TriggerActionDiscrete>();

      // ...because discrete actions with a timer need updated every frame
      void Update()
      {
        foreach (TriggerActionDiscrete action in actionsNeedingUpdate)
        {
          action.Update();
        }
        actionsNeedingUpdate.RemoveAll((a) => !a.timerActive);
      }

      public void AddActionToManager(TriggerActionDiscrete action)
      {
        if (action.useTimer)
        {
          actionsNeedingUpdate.Add(action);
        }
      }

      void TriggerHandler.RemoveTrigger(Trigger t) {} // unused
      void TriggerHandler.DuplicateTrigger(Trigger t) {} // unused

      RectTransform TriggerHandler.CreateTriggerActionsUI()
      {
        return Instantiate(customTriggerActionsPrefab.transform as RectTransform);
      }

      RectTransform TriggerHandler.CreateTriggerActionMiniUI()
      {
        return Instantiate(customTriggerActionMiniPrefab.transform as RectTransform);
      }

      RectTransform TriggerHandler.CreateTriggerActionDiscreteUI()
      {
        return Instantiate(customTriggerActionDiscretePrefab.transform as RectTransform);
      }

      RectTransform TriggerHandler.CreateTriggerActionTransitionUI()
      {
        return Instantiate(customTriggerActionTransitionPrefab.transform as RectTransform);
      }

      void TriggerHandler.RemoveTriggerActionUI(RectTransform rt)
      {
        Destroy(rt?.gameObject);
      }
    }

    // Base class for easier handling of custom triggers
    public abstract class CustomTrigger : Trigger
    {
      public string name { get; protected set; }

      protected bool panelInitialized = false;
      protected Text text;

      protected abstract void InitPanel();

      public void OpenPanel()
      {
        if (!VaMUI.triggerUtilsInitialized || !VaMUI.triggerPrefabsLoaded)
        {
          SuperController.LogError("CustomTrigger: You need to call VaMUI.Init() before use.");
          return;
        }
        if (base.handler == null)
        {
          base.handler = VaMUI.triggerHandler;
        }
        if (!panelInitialized)
        {
          triggerActionsParent = VaMUI.script.UITransform;
          InitTriggerUI();
          OpenTriggerActionsPanel();
          text = triggerActionsPanel.Find("Panel").Find("Header Text").GetComponent<Text>();
          text.text = name;
          InitPanel();
          panelInitialized = true;
        }
        else
        {
          OpenTriggerActionsPanel();
          text.text = name;
        }
      }

      public void StoreJSON(JSONClass json)
      {
        json[name] = base.GetJSON();
      }

      protected void RestoreFromJSONInternal(JSONClass json)
      {
        base.RestoreFromJSON(json);
      }

      public override void RestoreFromJSON(JSONClass json)
      {
        base.RestoreFromJSON(json[name].AsObject);
      }
    }

    public class EventTrigger : CustomTrigger
    {
      public EventTrigger(string name)
      {
        this.name = name;
      }

      public EventTrigger Clone(string name = null)
      {
        name = name ?? this.name;
        EventTrigger newTrigger = new EventTrigger(name);
        JSONClass json = GetJSON();
        newTrigger.RestoreFromJSONInternal(json);
        return newTrigger;
      }

      protected override void InitPanel()
      {
        Transform content = triggerActionsPanel.Find("Content");
        content.Find("Tab1/Label").GetComponent<Text>().text = "Event Actions";
        content.Find("Tab2").gameObject.SetActive(false);
        content.Find("Tab3").gameObject.SetActive(false);
      }

      public void Trigger()
      {
        foreach (TriggerActionDiscrete action in discreteActionsStart)
        {
          action.Trigger(force: true);
          VaMUI.triggerHandler.AddActionToManager(action);
        }
      }
    }

    public class ValueTrigger : CustomTrigger
    {
      public ValueTrigger(string name)
      {
        this.name = name;
      }

      public ValueTrigger Clone(string name = null)
      {
        name = name ?? this.name;
        ValueTrigger newTrigger = new ValueTrigger(name);
        JSONClass json = GetJSON();
        newTrigger.RestoreFromJSONInternal(json);
        return newTrigger;
      }

      protected override void InitPanel()
      {
        Transform content = triggerActionsPanel.Find("Content");
        content.Find("Tab2/Label").GetComponent<Text>().text = "Value Actions";
        content.Find("Tab2").GetComponent<Toggle>().isOn = true;
        content.Find("Tab1").gameObject.SetActive(false);
        content.Find("Tab3").gameObject.SetActive(false);
      }

      public void Trigger(float v)
      {
        _transitionInterpValue = Mathf.Clamp01(v);
        if (transitionInterpValueSlider != null)
        {
          transitionInterpValueSlider.value = _transitionInterpValue;
        }
        foreach (TriggerActionTransition action in transitionActions)
        {
          action.TriggerInterp(_transitionInterpValue, true);
        }
      }
    }
  }

  // UIDynamic classes for holding UI item properties
  public class UIDynamicBase : UIDynamic
  {
    public RectTransform rectTransform;
    public LayoutElement layout;
  }

  public class UIDynamicButtonPair : UIDynamicBase
  {
    public UIDynamicButton leftButton;
    public UIDynamicButton rightButton;
  }

  public class UIDynamicInfoText : UIDynamicBase
  {
    public RectTransform textRect;
    public Text text;
    public Image bgImage;
  }

  public class UIDynamicLabelWithX : UIDynamicBase
  {
    public Text label;
    public UIDynamicButton button;
  }

  public class UIDynamicTabBar : UIDynamicBase
  {
    public List<UIDynamicButton> buttons = new List<UIDynamicButton>();
  }

  public class UIDynamicHorizontalLine : UIDynamicBase
  {

  }

  public class UIDynamicCustomSlider : UIDynamicBase
  {
    public Slider slider;
    public Text label;
    public SetTextFromFloat textFormatter;
    public Text m1ButtonText;
    public Text m2ButtonText;
    public Text m3ButtonText;
    public Text p1ButtonText;
    public Text p2ButtonText;
    public Text p3ButtonText;
    public Text mRangeButtonText;
    public Text pRangeButtonText;
    public Button defaultButton;
    public Button m1Button;
    public Button m2Button;
    public Button m3Button;
    public Button p1Button;
    public Button p2Button;
    public Button p3Button;
    public Button mRangeButton;
    public Button pRangeButton;

    public JSONStorableFloat storable = null;
    public bool fixedRange = false;
    public bool exponentialRangeIncrement = false;
    public bool integer = false;
    public bool interactable = true;

    public float defaultValue;
    public float defaultMin;
    public float defaultMax;

    float incrementRange;

    void Start()
    {
      textFormatter.floatFormat = integer ? "F0" : "F3";
      textFormatter.enabled = false;
      textFormatter.enabled = true;
      slider.wholeNumbers = integer;

      if (fixedRange)
      {
        mRangeButton.gameObject.SetActive(false);
        pRangeButton.gameObject.SetActive(false);
      }

      if (!interactable)
      {
        mRangeButton.gameObject.SetActive(false);
        pRangeButton.gameObject.SetActive(false);
        defaultButton.gameObject.SetActive(false);
        m1Button.gameObject.SetActive(false);
        m2Button.gameObject.SetActive(false);
        m3Button.gameObject.SetActive(false);
        p1Button.gameObject.SetActive(false);
        p2Button.gameObject.SetActive(false);
        p3Button.gameObject.SetActive(false);
      }

      if (storable == null) return;
      RecalculateIncrementRange();
      AssignLabels();
      AssignCallbacks();
    }

    void RecalculateIncrementRange()
    {
      float maxRange = Mathf.Max(Math.Abs(storable.max), Math.Abs(storable.min));
      incrementRange = VaMMath.NextPowerOf10(maxRange) / 10f;
      incrementRange = Mathf.Clamp(incrementRange, 1f, 10000f);
    }

    void AssignLabels()
    {
      label.text = storable.name;
      m1ButtonText.text = $"-{incrementRange / 100f}";
      m2ButtonText.text = $"-{incrementRange / 10f}";
      m3ButtonText.text = $"-{incrementRange}";
      p1ButtonText.text = $"+{incrementRange / 100f}";
      p2ButtonText.text = $"+{incrementRange / 10f}";
      p3ButtonText.text = $"+{incrementRange}";
    }

    void AssignCallbacks()
    {
      m1Button.onClick.AddListener(() => { storable.val -= incrementRange / 100f; });
      m2Button.onClick.AddListener(() => { storable.val -= incrementRange / 10f; });
      m3Button.onClick.AddListener(() => { storable.val -= incrementRange; });
      p1Button.onClick.AddListener(() => { storable.val += incrementRange / 100f; });
      p2Button.onClick.AddListener(() => { storable.val += incrementRange / 10f; });
      p3Button.onClick.AddListener(() => { storable.val += incrementRange; });

      mRangeButton.onClick.AddListener(() =>
      {
        if (exponentialRangeIncrement)
        {
          VaMUtils.MultiplySliderRange(storable, true);
        }
        else
        {
          VaMUtils.AddSliderRange(storable, true);
        }
        RecalculateIncrementRange();
        AssignLabels();
      });

      pRangeButton.onClick.AddListener(() =>
      {
        if (exponentialRangeIncrement)
        {
          VaMUtils.MultiplySliderRange(storable, false);
        }
        else
        {
          VaMUtils.AddSliderRange(storable, false);
        }
        RecalculateIncrementRange();
        AssignLabels();
      });

      defaultButton.onClick.AddListener(() =>
      {
        storable.min = defaultMin;
        storable.max = defaultMax;
        storable.val = defaultValue;
        RecalculateIncrementRange();
        AssignLabels();
      });
    }
  }

  public class UIDynamicOnelineTextInput : UIDynamicBase
  {
    public Text label;
    public InputField input;
  }

  public class UIDynamicLabelWithToggle : UIDynamicBase
  {
    public Text label;
    public Toggle toggle;
  }
}
