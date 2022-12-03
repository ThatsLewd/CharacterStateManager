/* /////////////////////////////////////////////////////////////////////////////////////////////////
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
using Request = MeshVR.AssetLoader.AssetBundleFromFileRequest;
using AssetBundles;
using SimpleJSON;

namespace VaMUtils
{
  // ================================================================================================== //
  // ========================================== ENUMS/CONSTS ========================================== //
  // ================================================================================================== //
  public static class UIColor
  {
    public readonly static Color GREEN = new Color(0.5f, 1.0f, 0.5f);
    public readonly static Color RED = new Color(1.0f, 0.5f, 0.5f);
    public readonly static Color BLUE = new Color(0.5f, 0.5f, 1.0f);
    public readonly static Color YELLOW = new Color(1.0f, 1.0f, 0.5f);
    public readonly static Color WHITE = new Color(1.0f, 1.0f, 1.0f);
    public readonly static Color BLACK = new Color(0.0f, 0.0f, 0.0f);
    public readonly static Color TRANSPARENT = new Color(0.0f, 0.0f, 0.0f, 0.0f);
  }

  // Custom enums don't work in VaM, so here is a hack
  public partial struct UIColumn
  {
    public readonly static UIColumn LEFT = new UIColumn(0);
    public readonly static UIColumn RIGHT = new UIColumn(1);
  }

  // ============================================================================================== //
  // ========================================== UI UTILS ========================================== //
  // ============================================================================================== //
  // Usage:
  // - In script Init(), call:
  //       UIBuilder.Init(this, CreateUIElement);
  // - In script OnDestroy(), call:
  //       UIBuilder.Destroy();

  public static class UIBuilder
  {
    public static void Init(MVRScript script, CreateUIElement createUIElementCallback)
    {
      UIBuilder.script = script;
      createUIElement = createUIElementCallback;
    }

    public static void Destroy()
    {
      Utils.SafeDestroy(ref customOnelineTextInputPrefab);
      Utils.SafeDestroy(ref customLabelWithXPrefab);
      Utils.SafeDestroy(ref customButtonPairPrefab);
      Utils.SafeDestroy(ref customInfoTextPrefab);
      Utils.SafeDestroy(ref customSliderPrefab);
      Utils.SafeDestroy(ref backgroundElementPrefab);
      Utils.SafeDestroy(ref labelElementPrefab);
      Utils.SafeDestroy(ref textFieldElementPrefab);
      Utils.SafeDestroy(ref buttonElementPrefab);
    }

    // Create an action that other objects can see
    public static JSONStorableAction CreateAction(string name, JSONStorableAction.ActionCallback callback)
    {
      JSONStorableAction action = new JSONStorableAction(name, callback);
      script.RegisterAction(action);
      return action;
    }

    // Create default VaM toggle
    public static UIDynamicToggle CreateToggle(ref JSONStorableBool storable, UIColumn side, string label, bool defaultValue, JSONStorableBool.SetBoolCallback callback = null, bool register = false)
    {
      if (storable == null)
      {
        storable = new JSONStorableBool(label, defaultValue);
        if (register)
        {
          storable.storeType = JSONStorableParam.StoreType.Full;
          script.RegisterBool(storable);
        }
        if (callback != null)
        {
          storable.setCallbackFunction = callback;
        }
      }
      return CreateToggleFromStorable(storable, side);
    }
    public static UIDynamicToggle CreateToggleFromStorable(JSONStorableBool storable, UIColumn side)
    {
      UIDynamicToggle toggle = script.CreateToggle(storable, side == UIColumn.RIGHT);
      return toggle;
    }

    // Create default VaM string chooser
    public static UIDynamicPopup CreateStringChooser
    (
      ref JSONStorableStringChooser storable,
      UIColumn side,
      string label,
      List<string> initialChoices = null,
      bool noDefaultSelection = false,
      bool filterable = false,
      JSONStorableStringChooser.SetStringCallback callback = null,
      bool register = false
    )
    {
      if (storable == null)
      {
        if (initialChoices == null)
        {
          initialChoices = new List<string>();
        }
        string defaultValue = (!noDefaultSelection && initialChoices.Count > 0) ? initialChoices[0] : "";
        storable = new JSONStorableStringChooser(label, initialChoices, defaultValue, label);
        if (register)
        {
          storable.storeType = JSONStorableParam.StoreType.Full;
          script.RegisterStringChooser(storable);
        }
        if (callback != null)
        {
          storable.setCallbackFunction = callback;
        }
      }
      return CreateStringChooserFromStorable(storable, side, filterable);
    }
    public static UIDynamicPopup CreateStringChooserKeyVal
    (
      ref JSONStorableStringChooser storable,
      UIColumn side,
      string label,
      List<KeyValuePair<string, string>> initialKeyValues = null,
      bool noDefaultSelection = false,
      bool filterable = false,
      JSONStorableStringChooser.SetStringCallback callback = null,
      bool register = false
    )
    {
      if (storable == null)
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
        string defaultValue = (!noDefaultSelection && initialChoices.Count > 0) ? initialChoices[0] : "";
        storable = new JSONStorableStringChooser(label, initialChoices, initialDisplayChoices, defaultValue, label);
        if (register)
        {
          storable.storeType = JSONStorableParam.StoreType.Full;
          script.RegisterStringChooser(storable);
        }
        if (callback != null)
        {
          storable.setCallbackFunction = callback;
        }
      }
      return CreateStringChooserFromStorable(storable, side, filterable);
    }
    public static UIDynamicPopup CreateStringChooserFromStorable(JSONStorableStringChooser storable, UIColumn side, bool filterable = false)
    {
      UIDynamicPopup popup;
      if (filterable)
      {
        popup = script.CreateFilterablePopup(storable, side == UIColumn.RIGHT);
      }
      else
      {
        popup = script.CreateScrollablePopup(storable, side == UIColumn.RIGHT);
      }
      return popup;
    }

    // Create default VaM Button
    public static UIDynamicButton CreateButton(UIColumn side, string label, UnityAction callback, Color? color = null)
    {
      UIDynamicButton button = script.CreateButton(label, side == UIColumn.RIGHT);
      button.button.onClick.AddListener(callback);
      if (color != null)
      {
        button.buttonColor = color.Value;
      }
      return button;
    }

    // Create default VaM text field with scrolling
    public static UIDynamicTextField CreateInfoText(UIColumn side, string text, float height)
    {
      JSONStorableString storable = new JSONStorableString("Info", text);
      UIDynamicTextField textfield = script.CreateTextField(storable, side == UIColumn.RIGHT);
      LayoutElement layout = textfield.GetComponent<LayoutElement>();
      layout.minHeight = height;
      return textfield;
    }

    // Create spacer
    public static UIDynamic CreateSpacer(UIColumn side, float height = 20f)
    {
      UIDynamic spacer = script.CreateSpacer(side == UIColumn.RIGHT);
      spacer.height = height;
      return spacer;
    }

    // Create default VaM color picker
    public static UIDynamicColorPicker CreateColorPicker(ref JSONStorableColor storable, UIColumn side, string label, Color defaultValue, JSONStorableColor.SetHSVColorCallback callback = null, bool register = false)
    {
      if (storable == null)
      {
        HSVColor hsvColor = HSVColorPicker.RGBToHSV(defaultValue.r, defaultValue.g, defaultValue.b);
        storable = new JSONStorableColor(label, hsvColor);
        if (register)
        {
          storable.storeType = JSONStorableParam.StoreType.Full;
          script.RegisterColor(storable);
        }
        if (callback != null)
        {
          storable.setCallbackFunction = callback;
        }
      }
      return CreateColorPickerFromStorable(storable, side);
    }
    public static UIDynamicColorPicker CreateColorPickerFromStorable(JSONStorableColor storable, UIColumn side)
    {
      UIDynamicColorPicker picker = script.CreateColorPicker(storable, side == UIColumn.RIGHT);
      return picker;
    }

    // Create texture chooser -- note that you are responsible for destroying the texture when you don't need it anymore.
    public static void CreateTextureChooser(ref JSONStorableUrl storable, UIColumn side, string label, string defaultValue, TextureSettings settings, TextureSetCallback callback = null, bool register = false)
    {
      if (storable == null)
      {
        storable = new JSONStorableUrl(label, string.Empty, (string url) => { QueueLoadTexture(url, settings, callback); }, "jpg|png|tif|tiff");
        if (register)
        {
          storable.storeType = JSONStorableParam.StoreType.Full;
          script.RegisterUrl(storable);
        }
        if (!string.IsNullOrEmpty(defaultValue))
        {
          storable.SetFilePath(defaultValue);
        }
      }
      CreateTextureChooserFromStorable(storable, side);
    }
    public static void CreateTextureChooserFromStorable(JSONStorableUrl storable, UIColumn side)
    {
      string label = storable.name;
      UIDynamicButton button = script.CreateButton("Browse " + label, side == UIColumn.RIGHT);
      UIDynamicTextField textfield = script.CreateTextField(storable, side == UIColumn.RIGHT);
      textfield.UItext.alignment = TextAnchor.MiddleRight;
      textfield.UItext.horizontalOverflow = HorizontalWrapMode.Overflow;
      textfield.UItext.verticalOverflow = VerticalWrapMode.Truncate;
      LayoutElement layout = textfield.GetComponent<LayoutElement>();
      layout.preferredHeight = layout.minHeight = 35;
      textfield.height = 35;
      storable.RegisterFileBrowseButton(button.button);
    }

    // Create asset bundle chooser
    public static void CreateAssetBundleChooser(ref JSONStorableUrl storable, UIColumn side, string label, string defaultValue, string fileExtensions, JSONStorableString.SetStringCallback callback = null, bool register = false)
    {
      if (storable == null)
      {
        storable = new JSONStorableUrl(label, defaultValue, fileExtensions);
        if (register)
        {
          storable.storeType = JSONStorableParam.StoreType.Full;
          script.RegisterUrl(storable);
        }
        if (!string.IsNullOrEmpty(defaultValue))
        {
          storable.SetFilePath(defaultValue);
        }
        if (callback != null)
        {
          storable.setCallbackFunction = callback;
        }
      }
      CreateAssetBundleChooserFromStorable(storable, side);
    }
    public static void CreateAssetBundleChooserFromStorable(JSONStorableUrl storable, UIColumn side)
    {
      string label = storable.name;
      UIDynamicButton button = script.CreateButton("Select " + label, side == UIColumn.RIGHT);
      UIDynamicTextField textfield = script.CreateTextField(storable, side == UIColumn.RIGHT);
      textfield.UItext.alignment = TextAnchor.MiddleRight;
      textfield.UItext.horizontalOverflow = HorizontalWrapMode.Overflow;
      textfield.UItext.verticalOverflow = VerticalWrapMode.Truncate;
      LayoutElement layout = textfield.GetComponent<LayoutElement>();
      layout.preferredHeight = layout.minHeight = 35;
      textfield.height = 35;
      storable.RegisterFileBrowseButton(button.button);
    }

    // Create a custom slider with less sucky behavior (Hint: use C# named params for optional args)
    private static GameObject customSliderPrefab;
    public static UIDynamicCustomSlider CreateSlider
    (
      ref JSONStorableFloat storable,
      UIColumn side,
      string label,
      float defaultValue,
      float defaultRange,
      bool allowNegative = false,
      bool fixedRange = false,
      bool exponentialRangeIncrement = false,
      bool integer = false,
      bool interactable = true,
      JSONStorableFloat.SetFloatCallback callback = null,
      bool register = false
    )
    {
      float defaultMin = allowNegative ? -defaultRange : 0f;
      float defaultMax = defaultRange;
      return CreateSlider(ref storable, side, label, defaultValue, defaultMin, defaultMax, fixedRange, exponentialRangeIncrement, integer, interactable, callback, register);
    }
    public static UIDynamicCustomSlider CreateSlider
    (
      ref JSONStorableFloat storable,
      UIColumn side,
      string label,
      float defaultValue,
      float defaultMin,
      float defaultMax,
      bool fixedRange = false,
      bool exponentialRangeIncrement = false,
      bool integer = false,
      bool interactable = true,
      JSONStorableFloat.SetFloatCallback callback = null,
      bool register = false
    )
    {
      if (storable == null)
      {
        storable = new JSONStorableFloat(label, defaultValue, defaultMin, defaultMax, true, interactable);
        if (register)
        {
          storable.storeType = JSONStorableParam.StoreType.Full;
          script.RegisterFloat(storable);
        }
        if (callback != null)
        {
          storable.setCallbackFunction = callback;
        }
      }
      return CreateSliderFromStorable(storable, side, defaultValue, defaultMin, defaultMax, fixedRange, exponentialRangeIncrement, integer, interactable);
    }
    public static UIDynamicCustomSlider CreateSliderFromStorable(JSONStorableFloat storable, UIColumn side, float defaultValue, float defaultMin, float defaultMax, bool fixedRange = false, bool exponentialRangeIncrement = false, bool integer = false, bool interactable = true)
    {
      if (customSliderPrefab == null)
      {
        Transform basePrefab = script.manager.configurableSliderPrefab;
        customSliderPrefab = UnityEngine.Object.Instantiate(basePrefab.gameObject);
        customSliderPrefab.name = "Slider";

        UnityEngine.Object.DestroyImmediate(customSliderPrefab.GetComponent<UIDynamicSlider>());
        UnityEngine.Object.DestroyImmediate(customSliderPrefab.transform.Find("RangePanel").gameObject);

        Slider slider = customSliderPrefab.transform.Find("Slider").GetComponent<Slider>();
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
        Transform t = createUIElement(customSliderPrefab.transform, side == UIColumn.RIGHT);
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

    // Create one-line text input with a label
    private static GameObject customOnelineTextInputPrefab;
    public static UIDynamicOnelineTextInput CreateOnelineTextInput(ref JSONStorableString storable, UIColumn side, string label, string defaultValue = "", JSONStorableString.SetStringCallback callback = null, bool register = false)
    {
      if (storable == null)
      {
        storable = new JSONStorableString(label, defaultValue);
        if (register)
        {
          storable.storeType = JSONStorableParam.StoreType.Full;
          script.RegisterString(storable);
        }
        if (callback != null)
        {
          storable.setCallbackFunction = callback;
        }
      }
      return CreateOnelineTextInputFromStorable(storable, side);
    }
    public static UIDynamicOnelineTextInput CreateOnelineTextInputFromStorable(JSONStorableString storable, UIColumn side)
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
        Transform t = createUIElement(customOnelineTextInputPrefab.transform, side == UIColumn.RIGHT);
        UIDynamicOnelineTextInput uid = t.GetComponent<UIDynamicOnelineTextInput>();
        uid.label.text = label;
        storable.inputField = uid.input;
        uid.gameObject.SetActive(true);
        return uid;
      }
    }

    // Create label that has an X button on the right side
    private static GameObject customLabelWithXPrefab;
    public static UIDynamicLabelWithX CreateLabelWithX(UIColumn side, string label, UnityAction callback)
    {
      if (customLabelWithXPrefab == null)
      {
        UIDynamicLabelWithX uid = CreateUIDynamicPrefab<UIDynamicLabelWithX>("LabelWithX", 50f);
        customLabelWithXPrefab = uid.gameObject;

        RectTransform background = InstantiateBackground(uid.transform);

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
        Transform t = createUIElement(customLabelWithXPrefab.transform, side == UIColumn.RIGHT);
        UIDynamicLabelWithX uid = t.GetComponent<UIDynamicLabelWithX>();
        uid.label.text = label;
        uid.button.button.onClick.AddListener(callback);
        uid.gameObject.SetActive(true);
        return uid;
      }
    }

    // Create label that has a toggle on the right side
    // Not much different than a normal toggle -- but a good example of how to do a custom toggle
    private static GameObject customLabelWithTogglePrefab;
    public static UIDynamicLabelWithToggle CreateLabelWithToggle(ref JSONStorableBool storable, UIColumn side, string label, bool defaultValue, JSONStorableBool.SetBoolCallback callback = null, bool register = false)
    {
      if (storable == null)
      {
        storable = new JSONStorableBool(label, defaultValue);
        if (register)
        {
          storable.storeType = JSONStorableParam.StoreType.Full;
          script.RegisterBool(storable);
        }
        if (callback != null)
        {
          storable.setCallbackFunction = callback;
        }
      }
      return CreateLabelWithToggleFromStorable(storable, side);
    }
    public static UIDynamicLabelWithToggle CreateLabelWithToggleFromStorable(JSONStorableBool storable, UIColumn side)
    {
      if (customLabelWithTogglePrefab == null)
      {
        UIDynamicLabelWithToggle uid = CreateUIDynamicPrefab<UIDynamicLabelWithToggle>("LabelWithToggle", 50f);
        customLabelWithTogglePrefab = uid.gameObject;

        RectTransform background = InstantiateBackground(uid.transform);

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
        Transform t = createUIElement(customLabelWithTogglePrefab.transform, side == UIColumn.RIGHT);
        UIDynamicLabelWithToggle uid = t.GetComponent<UIDynamicLabelWithToggle>();
        uid.label.text = label;
        storable.RegisterToggle(uid.toggle);
        uid.gameObject.SetActive(true);
        return uid;
      }
    }

    // Create two buttons on one line
    private static GameObject customButtonPairPrefab;
    public static UIDynamicButtonPair CreateButtonPair(UIColumn side, string leftLabel, UnityAction leftCallback, string rightLabel, UnityAction rightCallback)
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
        Transform t = createUIElement(customButtonPairPrefab.transform, side == UIColumn.RIGHT);
        UIDynamicButtonPair uid = t.GetComponent<UIDynamicButtonPair>();
        uid.leftButton.buttonText.text = leftLabel;
        uid.leftButton.button.onClick.AddListener(leftCallback);
        uid.rightButton.buttonText.text = rightLabel;
        uid.rightButton.button.onClick.AddListener(rightCallback);
        uid.gameObject.SetActive(true);
        return uid;
      }
    }

    // Create an info textbox with scrolling disabled
    private static GameObject customInfoTextPrefab;
    public static UIDynamicInfoText CreateInfoTextNoScroll(UIColumn side, string text, float height, bool background = true)
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
        Transform t = createUIElement(customInfoTextPrefab.transform, side == UIColumn.RIGHT);
        UIDynamicInfoText uid = t.GetComponent<UIDynamicInfoText>();
        uid.text.text = text;
        uid.layout.minHeight = height;
        uid.layout.preferredHeight = height;
        if (!background)
        {
          uid.bgImage.color = UIColor.TRANSPARENT;
          uid.textRect.offsetMin = new Vector2(8f, 0f);
          uid.textRect.offsetMax = new Vector2(-8f, 0f);
        }
        uid.gameObject.SetActive(true);
        return uid;
      }
    }

    // Create an info textbox with scrolling disabled with a specified number of lines (default text size only)
    public static UIDynamicInfoText CreateInfoTextNoScroll(UIColumn side, string text, int lines, bool background = true)
    {
      return CreateInfoTextNoScroll(side, text, lines * 32f + 8f, background);
    }

    // Creates a one-line header with automatic styling
    public static UIDynamicInfoText CreateHeaderText(UIColumn side, string text, float size = 50f)
    {
      UIDynamicInfoText uid = CreateInfoTextNoScroll(side, $"<size={size * 0.85f}><b>{text}</b></size>", size, background: false);
      return uid;
    }

    // Create a list of buttons that spans both columns
    // NOTE that this creates a prefab, which you should clean up when your script exits
    // NOTE that this also means the tab bar is not dynamic -- it is setup once and the same prefab is reused
    public static UIDynamicTabBar CreateTabBar(ref GameObject prefab, UIColumn anchorSide, string[] menuItems, TabClickCallback callback, int tabsPerRow = 6)
    {
      if (prefab == null)
      {
        const float rowHeight = 50f;
        const float rowWidth = 1060f;
        const float spacing = 5f;

        int numRows = Mathf.CeilToInt((float)menuItems.Length / (float)tabsPerRow);
        float totalHeight = numRows * (rowHeight + spacing);
        float tabWidth = ((rowWidth + spacing) / tabsPerRow) - spacing;
        float tabHeight = rowHeight;

        UIDynamicTabBar uid = CreateUIDynamicPrefab<UIDynamicTabBar>("TabBar", 0f);
        prefab = uid.gameObject;

        uid.layout.minHeight = uid.layout.preferredHeight = totalHeight;
        uid.layout.minWidth = uid.layout.preferredWidth = rowWidth;

        for (int i = 0; i < menuItems.Length; i++)
        {
          int col = i % tabsPerRow;
          int row = i / tabsPerRow;

          float xOffset = col * (tabWidth + spacing);
          if (anchorSide == UIColumn.RIGHT)
          {
            xOffset -= rowWidth / 2 + 9f;
          }
          float yOffset = -row * (rowHeight + spacing);

          RectTransform tabRect = InstantiateButton(prefab.transform);
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
        Transform t = createUIElement(prefab.transform, anchorSide == UIColumn.RIGHT);
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


    // ======================================================================================================= //
    // ========================================== CUSTOM UI HELPERS ========================================== //
    // ======================================================================================================= //
    // These methods instantiate basic UI elements to make building custom components easier
    // See custom UI methods above for examples

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
      return InstantiateWithSameName(rectElementPrefab.transform as RectTransform, parent);
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
      return InstantiateWithSameName(backgroundElementPrefab.transform as RectTransform, parent);
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
      return InstantiateWithSameName(labelElementPrefab.transform as RectTransform, parent);
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
      return InstantiateWithSameName(textFieldElementPrefab.transform as RectTransform, parent);
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
      return InstantiateWithSameName(buttonElementPrefab.transform as RectTransform, parent);
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
        RectTransform toggle = InstantiateWithSameName(toggleElementPrefab.transform as RectTransform, parent);
        RectTransform backgroundRect = toggle.Find("Background") as RectTransform;
        backgroundRect.offsetMin = new Vector2(0f, -size);
        backgroundRect.offsetMax = new Vector2(size, 0f);
        return toggle;
      }
    }

    private static void ResetRectTransform(RectTransform transform)
    {
      transform.anchoredPosition = new Vector2(0f, 0f);
      transform.anchorMin = new Vector2(0f, 0f);
      transform.anchorMax = new Vector2(1f, 1f);
      transform.offsetMin = new Vector2(0f, 0f);
      transform.offsetMax = new Vector2(0f, 0f);
      transform.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void ResetLayoutElement(LayoutElement layout, float height = 0f, bool flexWidth = true)
    {
      layout.flexibleWidth = flexWidth ? 1f : -1f;
      layout.flexibleHeight = -1f;
      layout.minWidth = -1f;
      layout.minHeight = height;
      layout.preferredWidth = -1f;
      layout.preferredHeight = height;
    }

    private static T InstantiateWithSameName<T>(T original, Transform parent) where T : UnityEngine.Object
    {
      T obj = UnityEngine.Object.Instantiate(original, parent);
      obj.name = original.name;
      return obj;
    }

    private static void QueueLoadTexture(string url, TextureSettings settings, TextureSetCallback callback)
    {
      if (ImageLoaderThreaded.singleton == null)
        return;
      if (string.IsNullOrEmpty(url))
        return;

      ImageLoaderThreaded.QueuedImage queuedImage = new ImageLoaderThreaded.QueuedImage()
      {
        imgPath = url,
        forceReload = true,
        skipCache = true,
        compress = settings.compress,
        createMipMaps = settings.createMipMaps,
        isNormalMap = settings.isNormalMap,
        linear = settings.linearColor,
        createAlphaFromGrayscale = settings.createAlphaFromGrayscale,
        createNormalFromBump = settings.createNormalFromBump,
        bumpStrength = settings.bumpStrength,
        isThumbnail = false,
        fillBackground = false,
        invert = false,
        callback = (ImageLoaderThreaded.QueuedImage qi) =>
        {
          Texture2D tex = qi.tex;
          if (tex != null)
          {
            tex.wrapMode = settings.wrapMode;
            tex.filterMode = settings.filterMode;
            tex.anisoLevel = settings.anisoLevel;
          }
          if (callback != null)
          {
            callback(tex);
          }
        }
      };
      ImageLoaderThreaded.singleton.QueueImage(queuedImage);
    }

    private static MVRScript script;
    private static CreateUIElement createUIElement;
  }

  // =================================================================================================== //
  // ========================================== TRIGGER UTILS ========================================== //
  // =================================================================================================== //
  // Usage:
  // - In script Init(), call:
  //       TriggerUtil.Init(this);
  // - In script OnDestroy(), call:
  //       TriggerUtil.Destroy();
  //
  // Credit to AcidBubbles for figuring out how to do custom triggers.

  public static class TriggerUtil
  {
    public static bool Loaded { get; private set; }

    public static void Init(MVRScript script)
    {
      TriggerUtil.script = script;
      handler = new SimpleTriggerHandler();
      SuperController.singleton.StartCoroutine(LoadAssets());
    }

    public static void Destroy()
    {
      // nothing for now
    }

    // Create a trigger
    public static T Create<T>(string name, string secondaryName = null) where T : CustomTrigger, new()
    {
      T trigger = new T();
      trigger.Initialize(script, handler, name, secondaryName);
      return trigger;
    }

    // Clone a trigger
    public static T Clone<T>(T other) where T : CustomTrigger, new()
    {
      T trigger = new T();
      trigger.Initialize(other);
      return trigger;
    }

    // Restore an existing trigger from JSON
    // These restore methods are a little clunky because of the way triggers are stored
    public static void RestoreFromJSON<T>(ref T trigger, JSONClass jc, bool setMissingToDefault) where T : CustomTrigger, new()
    {
      trigger.RestoreFromJSON(jc, script.subScenePrefix, script.mergeRestore, setMissingToDefault);
    }

    // Restore a trigger from JSON by name
    // These restore methods are a little clunky because of the way triggers are stored
    public static void RestoreFromJSON<T>(out T trigger, string name, JSONClass jc, bool setMissingToDefault) where T : CustomTrigger, new()
    {
      trigger = new T();
      trigger.Initialize(script, handler, name);
      trigger.RestoreFromJSON(jc, script.subScenePrefix, script.mergeRestore, setMissingToDefault);
    }

    private static IEnumerator LoadAssets()
    {
      foreach (var x in LoadAsset("z_ui2", "TriggerActionsPanel", p => ourTriggerActionsPrefab = p))
        yield return x;
      foreach (var x in LoadAsset("z_ui2", "TriggerActionMiniPanel", p => ourTriggerActionMiniPrefab = p))
        yield return x;
      foreach (var x in LoadAsset("z_ui2", "TriggerActionDiscretePanel", p => ourTriggerActionDiscretePrefab = p))
        yield return x;
      foreach (var x in LoadAsset("z_ui2", "TriggerActionTransitionPanel", p => ourTriggerActionTransitionPrefab = p))
        yield return x;

      Loaded = true;
    }

    private static IEnumerable LoadAsset(string assetBundleName, string assetName, Action<RectTransform> assign)
    {
      AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(assetBundleName, assetName, typeof(GameObject));
      if (request == null)
        throw new NullReferenceException($"Request for {assetName} in {assetBundleName} assetbundle failed: Null request.");
      yield return request;
      GameObject go = request.GetAsset<GameObject>();
      if (go == null)
        throw new NullReferenceException($"Request for {assetName} in {assetBundleName} assetbundle failed: Null GameObject.");
      RectTransform prefab = go.GetComponent<RectTransform>();
      if (prefab == null)
        throw new NullReferenceException($"Request for {assetName} in {assetBundleName} assetbundle failed: Null RectTansform.");
      assign(prefab);
    }

    private static MVRScript script;
    private static SimpleTriggerHandler handler;
    private static RectTransform ourTriggerActionsPrefab;
    private static RectTransform ourTriggerActionMiniPrefab;
    private static RectTransform ourTriggerActionDiscretePrefab;
    private static RectTransform ourTriggerActionTransitionPrefab;

    // Helper class since we need a non-static handler
    private class SimpleTriggerHandler : TriggerHandler
    {
      void TriggerHandler.RemoveTrigger(Trigger t)
      {
        // unused
      }

      void TriggerHandler.DuplicateTrigger(Trigger t)
      {
        // unused
      }

      RectTransform TriggerHandler.CreateTriggerActionsUI()
      {
        return UnityEngine.Object.Instantiate(ourTriggerActionsPrefab);
      }

      RectTransform TriggerHandler.CreateTriggerActionMiniUI()
      {
        return UnityEngine.Object.Instantiate(ourTriggerActionMiniPrefab);
      }

      RectTransform TriggerHandler.CreateTriggerActionDiscreteUI()
      {
        return UnityEngine.Object.Instantiate(ourTriggerActionDiscretePrefab);
      }

      RectTransform TriggerHandler.CreateTriggerActionTransitionUI()
      {
        RectTransform rt = UnityEngine.Object.Instantiate(ourTriggerActionTransitionPrefab);
        rt.GetComponent<TriggerActionTransitionUI>().startWithCurrentValToggle.gameObject.SetActive(false);
        return rt;
      }

      void TriggerHandler.RemoveTriggerActionUI(RectTransform rt)
      {
        UnityEngine.Object.Destroy(rt?.gameObject);
      }
    }
  }

  // ============================================================================================================ //
  // ========================================== TRIGGER HELPER CLASSES ========================================== //
  // ============================================================================================================ //
  // You shouldn't need to instantiate these directly -- use TriggerUtil.Create()

  // Base class for easier handling of custom triggers.
  public abstract class CustomTrigger : Trigger
  {
    private string _name;
    public string name
    {
      get { return _name; }
      set { _name = value; rebuildPanel = true; }
    }

    private string _secondaryName;
    public string secondaryName
    {
      get { return _secondaryName; }
      set { _secondaryName = value; rebuildPanel = true; }
    }

    private MVRScript script;
    private bool initialized = false;
    private bool rebuildPanel = true;

    public CustomTrigger() { }

    public void Initialize(MVRScript script, TriggerHandler handler, string name, string secondaryName = null)
    {
      this.name = name;
      this.secondaryName = secondaryName;
      this.script = script;
      base.handler = handler;
      this.initialized = true;
    }

    public void Initialize(CustomTrigger other)
    {
      this.name = other.name;
      this.secondaryName = other.secondaryName;
      this.script = other.script;
      base.handler = other.handler;
      this.initialized = true;

      JSONClass jc = other.GetJSON(script.subScenePrefix);
      base.RestoreFromJSON(jc, script.subScenePrefix, false);
    }

    public void OpenPanel()
    {
      if (!TriggerUtil.Loaded)
      {
        SuperController.LogError("CustomTrigger: You need to call TriggerUtil.Init() before use.");
        return;
      }
      if (!initialized)
      {
        SuperController.LogError("CustomTrigger: Trigger is not initialized. Use TriggerUtil.Create() to instantiate triggers.");
        return;
      }

      triggerActionsParent = script.UITransform;
      InitTriggerUI();
      OpenTriggerActionsPanel();
      if (rebuildPanel)
      {
        Transform panel = triggerActionsPanel.Find("Panel");
        panel.Find("Header Text").GetComponent<Text>().text = name;
        Transform secondaryHeader = panel.Find("Trigger Name Text");
        secondaryHeader.gameObject.SetActive(!string.IsNullOrEmpty(secondaryName));
        secondaryHeader.GetComponent<Text>().text = secondaryName;

        InitPanel();
        rebuildPanel = false;
      }
    }

    protected abstract void InitPanel();

    public void RestoreFromJSON(JSONClass jc, string subScenePrefix, bool isMerge, bool setMissingToDefault)
    {
      if (jc.HasKey(name))
      {
        JSONClass tc = jc[name].AsObject;
        if (tc != null)
          base.RestoreFromJSON(tc, subScenePrefix, isMerge);
      }
      else if (setMissingToDefault)
      {
        base.RestoreFromJSON(new JSONClass());
      }
    }
  }

  // Wrapper for easier handling of custom event triggers.
  public class EventTrigger : CustomTrigger
  {
    public EventTrigger() : base() { }

    protected override void InitPanel()
    {
      Transform content = triggerActionsPanel.Find("Content");
      content.Find("Tab1/Label").GetComponent<Text>().text = "Event Actions";
      content.Find("Tab2").gameObject.SetActive(false);
      content.Find("Tab3").gameObject.SetActive(false);
    }

    public void Trigger()
    {
      active = true;
      active = false;
    }

    public void Trigger(List<TriggerActionDiscrete> actionsNeedingUpdateOut)
    {
      Trigger();
      for (int i = 0; i < discreteActionsStart.Count; ++i)
      {
        if (discreteActionsStart[i].timerActive)
          actionsNeedingUpdateOut.Add(discreteActionsStart[i]);
      }
    }
  }

  // Wrapper for easier handling of custom float triggers.
  public class FloatTrigger : CustomTrigger
  {
    public FloatTrigger() : base() { }

    protected override void InitPanel()
    {
      Transform content = triggerActionsPanel.Find("Content");
      content.Find("Tab2/Label").GetComponent<Text>().text = "Value Actions";
      content.Find("Tab3/Label").GetComponent<Text>().text = "Event Actions";
      content.Find("Tab2").GetComponent<Toggle>().isOn = true;
      content.Find("Tab1").gameObject.SetActive(false);
    }

    public void Trigger(float v)
    {
      _transitionInterpValue = Mathf.Clamp01(v);
      if (transitionInterpValueSlider != null)
        transitionInterpValueSlider.value = _transitionInterpValue;
      for (int i = 0; i < transitionActions.Count; ++i)
        transitionActions[i].TriggerInterp(_transitionInterpValue, true);
      for (int i = 0; i < discreteActionsEnd.Count; ++i)
        discreteActionsEnd[i].Trigger();
    }

    public void Trigger(float v, List<TriggerActionDiscrete> actionsNeedingUpdateOut)
    {
      Trigger(v);
      for (int i = 0; i < discreteActionsEnd.Count; ++i)
      {
        if (discreteActionsEnd[i].timerActive)
          actionsNeedingUpdateOut.Add(discreteActionsEnd[i]);
      }
    }
  }

  // =================================================================================================== //
  // ========================================== GENERAL UTILS ========================================== //
  // =================================================================================================== //
  public static class Utils
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
        int r = UnityEngine.Random.Range(0, length);
        str.Append(IDAlphabet[r]);
      }

      return str.ToString();
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

    // Add/Subtract slider range by current power of 10, clamping within range 1 - 100000
    public static void AddSliderRange(JSONStorableFloat storable, bool invert)
    {
      float maxRange = Mathf.Max(Math.Abs(storable.max), Math.Abs(storable.min));
      float newRange;
      float pow10 = CurrentPowerOf10(maxRange);
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
      float newRange = invert ? PrevPowerOf10(maxRange) : NextPowerOf10(maxRange);
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

    // Recursively log a gameobject and its children
    public static void LogGameObjectTree(GameObject obj, bool logComponents = true, bool logProps = false)
    {
      StringBuilder str = new StringBuilder();
      LogGameObjectTreeInternal(obj, 0, str, logComponents, logProps);
      str.Append("\n");
      SuperController.LogMessage(str.ToString());
    }
    public static void LogGameObjectTree(Transform obj, bool logComponents = true, bool logProps = false)
    {
      LogGameObjectTree(obj.gameObject, logComponents, logProps);
    }

    private static void LogGameObjectTreeInternal(GameObject obj, int depth, StringBuilder str, bool logComponents, bool logProps)
    {
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
        LogGameObjectTreeInternal(child.gameObject, depth + 1, str, logComponents, logProps);
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

  // ================================================================================================== //
  // ========================================== HELPER TYPES ========================================== //
  // ================================================================================================== //
  public delegate Transform CreateUIElement(Transform prefab, bool rightSide);
  public delegate void TextureSetCallback(Texture2D tex);
  public delegate void TabClickCallback(string tabName);

  // dumb custom enum stuff
  public partial struct UIColumn : IEquatable<UIColumn>
  {
    private int value;
    private UIColumn(int value)
    {
      this.value = value;
    }

    public bool Equals(UIColumn other)
    {
      return other.value == this.value;
    }

    public override bool Equals(object obj)
    {
      if (obj is UIColumn)
      {
        return this.Equals((UIColumn)obj);
      }
      return false;
    }

    public override int GetHashCode()
    {
      return value;
    }

    public static bool operator ==(UIColumn a, UIColumn b)
    {
      return a.Equals(b);
    }

    public static bool operator !=(UIColumn a, UIColumn b)
    {
      return !a.Equals(b);
    }
  }

  public class TextureSettings
  {
    public bool compress = false;
    public bool createMipMaps = true;
    public bool isNormalMap = false;
    public bool linearColor = true; // Using linear or sRGB color space.
    public bool createAlphaFromGrayscale = false;
    public bool createNormalFromBump = false;
    public float bumpStrength = 1.0f;
    public TextureWrapMode wrapMode = TextureWrapMode.Repeat;
    public FilterMode filterMode = FilterMode.Trilinear;
    public int anisoLevel = 5; // 0: Forced off, 1: Off, quality setting can override, 2-9: Anisotropic filtering levels.
  }

  public class AssetBundleAudioClip : NamedAudioClip
  {
    public AssetBundleAudioClip(Request aRequest, string aPath, string aName)
    {
      manager = null;
      sourceClip = aRequest.assetBundle?.LoadAsset<AudioClip>(aPath + aName);
      uid = aName;
      displayName = aName;
      category = "AssetBundle";
      destroyed = false;
    }
  }

  public class UIDynamicBase : UIDynamic
  {
    public RectTransform rectTransform;
    public LayoutElement layout;
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
      incrementRange = Utils.NextPowerOf10(maxRange) / 10f;
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
          Utils.MultiplySliderRange(storable, true);
        }
        else
        {
          Utils.AddSliderRange(storable, true);
        }
        RecalculateIncrementRange();
        AssignLabels();
      });

      pRangeButton.onClick.AddListener(() =>
      {
        if (exponentialRangeIncrement)
        {
          Utils.MultiplySliderRange(storable, false);
        }
        else
        {
          Utils.AddSliderRange(storable, false);
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

  public class UIDynamicLabelWithX : UIDynamicBase
  {
    public Text label;
    public UIDynamicButton button;
  }

  public class UIDynamicLabelWithToggle : UIDynamicBase
  {
    public Text label;
    public Toggle toggle;
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

  public class UIDynamicTabBar : UIDynamicBase
  {
    public List<UIDynamicButton> buttons = new List<UIDynamicButton>();
  }
}
