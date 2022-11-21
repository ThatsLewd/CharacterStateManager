/* /////////////////////////////////////////////////////////////////////////////////////////////////
Original utils 2021-03-13 by MacGruber
Collection of various utility functions.
https://www.patreon.com/MacGruber_Laboratory

Licensed under CC BY-SA after EarlyAccess ended. (see https://creativecommons.org/licenses/by-sa/4.0/)

Refactored, expanded 2022-10-24 by ThatsLewd
///////////////////////////////////////////////////////////////////////////////////////////////// */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
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
      ourCreateUIElement = createUIElementCallback;
    }

    public static void Destroy()
    {
      Utils.SafeDestroy(ref ourLabelWithInputPrefab);
      Utils.SafeDestroy(ref ourLabelWithXButtonPrefab);
      Utils.SafeDestroy(ref ourTextInfoPrefab);
      Utils.SafeDestroy(ref ourTwinButtonPrefab);
    }

    // Create VaM-UI Toggle button
    public static UIDynamicToggle CreateToggle(ref JSONStorableBool storable, UIColumn side, string label, bool defaultValue, JSONStorableBool.SetBoolCallback callback = null, bool register = true)
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
          storable.setCallbackFunction += callback;
        }
      }
      UIDynamicToggle toggle = script.CreateToggle(storable, side == UIColumn.RIGHT);
      return toggle;
    }

    // Create VaM-UI Float slider (hint: use c# named parameters for optional arguments)
    public static UIDynamicSlider CreateSlider(ref JSONStorableFloat storable, UIColumn side, string label, float defaultValue, float minValue, float maxValue, bool fixedRange = false, bool integer = false, bool interactable = true, JSONStorableFloat.SetFloatCallback callback = null, bool register = true)
    {
      if (storable == null)
      {
        storable = new JSONStorableFloat(label, defaultValue, minValue, maxValue, fixedRange, interactable);
        if (register)
        {
          storable.storeType = JSONStorableParam.StoreType.Full;
          script.RegisterFloat(storable);
        }
        if (callback != null)
        {
          storable.setCallbackFunction += callback;
        }
      }
      UIDynamicSlider slider = script.CreateSlider(storable, side == UIColumn.RIGHT);
      slider.rangeAdjustEnabled = !fixedRange;
      if (integer)
      {
        slider.slider.wholeNumbers = true;
        slider.valueFormat = "F0";
      }
      return slider;
    }

    // Create VaM-UI ColorPicker
    public static UIDynamicColorPicker CreateColor(ref JSONStorableColor storable, UIColumn side, string label, Color defaultValue, JSONStorableColor.SetHSVColorCallback callback = null, bool register = true)
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
          storable.setCallbackFunction += callback;
        }
      }
      UIDynamicColorPicker picker = script.CreateColorPicker(storable, side == UIColumn.RIGHT);
      return picker;
    }

    // Create VaM-UI StringChooser
    public static UIDynamicPopup CreateStringChooser(ref JSONStorableStringChooser storable, UIColumn side, string label, List<string> initialChoices, bool noDefaultSelection = false, JSONStorableStringChooser.SetStringCallback callback = null, bool register = true)
    {
      if (storable == null)
      {
        if (initialChoices == null)
        {
          initialChoices = new List<string>();
        }
        string defaultEntry = (!noDefaultSelection && initialChoices.Count > 0) ? initialChoices[0] : "";
        storable = new JSONStorableStringChooser(label, initialChoices, defaultEntry, label);
        if (register)
        {
          storable.storeType = JSONStorableParam.StoreType.Full;
          script.RegisterStringChooser(storable);
        }
        if (callback != null)
        {
          storable.setCallbackFunction += callback;
        }
      }
      UIDynamicPopup popup = script.CreateScrollablePopup(storable, side == UIColumn.RIGHT);
      return popup;
    }

    // Create VaM-UI TextureChooser. Note that you are responsible for destroying the texture when you don't need it anymore.
    public static void CreateTexture2DChooser(ref JSONStorableUrl storable, UIColumn side, string label, string defaultValue, TextureSettings settings, TextureSetCallback callback = null, bool register = true)
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

    // Create VaM-UI AssetBundleChooser.
    public static void CreateAssetBundleChooser(ref JSONStorableUrl storable, UIColumn side, string label, string defaultValue, string fileExtensions, JSONStorableString.SetStringCallback callback = null, bool register = true)
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
          storable.setCallbackFunction += callback;
        }
      }
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

    // Create VaM-UI InfoText field
    public static UIDynamicTextField CreateInfoText(UIColumn side, string text, float height)
    {
      JSONStorableString storable = new JSONStorableString("Info", text);
      UIDynamicTextField textfield = script.CreateTextField(storable, side == UIColumn.RIGHT);
      LayoutElement layout = textfield.GetComponent<LayoutElement>();
      layout.minHeight = height;
      return textfield;
    }

    public static UIDynamic CreateSpacer(UIColumn side, float height = 20f)
    {
      UIDynamic spacer = script.CreateSpacer(side == UIColumn.RIGHT);
      spacer.height = height;
      return spacer;
    }

    // Create VaM-UI button
    public static UIDynamicButton CreateButton(UIColumn side, string label, UnityAction callback)
    {
      UIDynamicButton button = script.CreateButton(label, side == UIColumn.RIGHT);
      button.button.onClick.AddListener(callback);
      return button;
    }

    // Create input action trigger
    public static void CreateAction(out JSONStorableAction action, string name, JSONStorableAction.ActionCallback callback)
    {
      action = new JSONStorableAction(name, callback);
      script.RegisterAction(action);
    }

    // Create one-line text input with label
    public static UIDynamicTextInput CreateTextInput(ref JSONStorableString storable, UIColumn side, string label, string defaultValue = "", JSONStorableString.SetStringCallback callback = null, bool register = false)
    {
      if (ourLabelWithInputPrefab == null)
      {
        ourLabelWithInputPrefab = new GameObject("LabelInput");
        ourLabelWithInputPrefab.SetActive(false);
        RectTransform rt = ourLabelWithInputPrefab.AddComponent<RectTransform>();
        rt.anchorMax = new Vector2(0, 1);
        rt.anchorMin = new Vector2(0, 1);
        rt.offsetMax = new Vector2(535, -500);
        rt.offsetMin = new Vector2(10, -600);
        LayoutElement le = ourLabelWithInputPrefab.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.minHeight = 45;
        le.minWidth = 350;
        le.preferredHeight = 45;
        le.preferredWidth = 500;

        RectTransform backgroundTransform = script.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
        backgroundTransform = UnityEngine.Object.Instantiate(backgroundTransform, ourLabelWithInputPrefab.transform);
        backgroundTransform.name = "Background";
        backgroundTransform.anchorMax = new Vector2(1, 1);
        backgroundTransform.anchorMin = new Vector2(0, 0);
        backgroundTransform.offsetMax = new Vector2(0, 0);
        backgroundTransform.offsetMin = new Vector2(0, -10);

        RectTransform labelTransform = script.manager.configurableScrollablePopupPrefab.transform.Find("Button/Text") as RectTransform;
        labelTransform = UnityEngine.Object.Instantiate(labelTransform, ourLabelWithInputPrefab.transform);
        labelTransform.name = "Text";
        labelTransform.anchorMax = new Vector2(0, 1);
        labelTransform.anchorMin = new Vector2(0, 0);
        labelTransform.offsetMax = new Vector2(155, -10);
        labelTransform.offsetMin = new Vector2(5, 0);
        Text labelText = labelTransform.GetComponent<Text>();
        labelText.text = "Name";
        labelText.color = Color.white;

        RectTransform inputTransform = script.manager.configurableTextFieldPrefab.transform as RectTransform;
        inputTransform = UnityEngine.Object.Instantiate(inputTransform, ourLabelWithInputPrefab.transform);
        inputTransform.anchorMax = new Vector2(1, 1);
        inputTransform.anchorMin = new Vector2(0, 0);
        inputTransform.offsetMax = new Vector2(-5, -5);
        inputTransform.offsetMin = new Vector2(160, -5);
        UIDynamicTextField textfield = inputTransform.GetComponent<UIDynamicTextField>();
        textfield.backgroundColor = Color.white;
        LayoutElement layout = textfield.GetComponent<LayoutElement>();
        layout.preferredHeight = layout.minHeight = 35;
        InputField inputfield = textfield.gameObject.AddComponent<InputField>();
        inputfield.textComponent = textfield.UItext;

        RectTransform textTransform = textfield.UItext.rectTransform;
        textTransform.anchorMax = new Vector2(1, 1);
        textTransform.anchorMin = new Vector2(0, 0);
        textTransform.offsetMax = new Vector2(-5, -5);
        textTransform.offsetMin = new Vector2(10, -5);

        UnityEngine.Object.Destroy(textfield);

        UIDynamicTextInput uid = ourLabelWithInputPrefab.AddComponent<UIDynamicTextInput>();
        uid.label = labelText;
        uid.input = inputfield;
      }
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
            storable.setCallbackFunction += callback;
          }
        }
        Transform t = ourCreateUIElement(ourLabelWithInputPrefab.transform, side == UIColumn.RIGHT);
        UIDynamicTextInput uid = t.gameObject.GetComponent<UIDynamicTextInput>();
        storable.inputField = uid.input;
        uid.label.text = label;
        t.gameObject.SetActive(true);
        return uid;
      }
    }

    // Create label that has an X button on the right side
    public static UIDynamicLabelXButton CreateLabelXButton(UIColumn side, string label, UnityAction callback)
    {
      if (ourLabelWithXButtonPrefab == null)
      {
        ourLabelWithXButtonPrefab = new GameObject("LabelXButton");
        ourLabelWithXButtonPrefab.SetActive(false);
        RectTransform rt = ourLabelWithXButtonPrefab.AddComponent<RectTransform>();
        rt.anchorMax = new Vector2(0, 1);
        rt.anchorMin = new Vector2(0, 1);
        rt.offsetMax = new Vector2(535, -500);
        rt.offsetMin = new Vector2(10, -600);
        LayoutElement le = ourLabelWithXButtonPrefab.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.minHeight = 50;
        le.minWidth = 350;
        le.preferredHeight = 50;
        le.preferredWidth = 500;

        RectTransform backgroundTransform = script.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
        backgroundTransform = UnityEngine.Object.Instantiate(backgroundTransform, ourLabelWithXButtonPrefab.transform);
        backgroundTransform.name = "Background";
        backgroundTransform.anchorMax = new Vector2(1, 1);
        backgroundTransform.anchorMin = new Vector2(0, 0);
        backgroundTransform.offsetMax = new Vector2(0, 0);
        backgroundTransform.offsetMin = new Vector2(0, -10);

        RectTransform buttonTransform = script.manager.configurableScrollablePopupPrefab.transform.Find("Button") as RectTransform;
        buttonTransform = UnityEngine.Object.Instantiate(buttonTransform, ourLabelWithXButtonPrefab.transform);
        buttonTransform.name = "Button";
        buttonTransform.anchorMax = new Vector2(1, 1);
        buttonTransform.anchorMin = new Vector2(1, 0);
        buttonTransform.offsetMax = new Vector2(0, 0);
        buttonTransform.offsetMin = new Vector2(-60, -10);
        Button buttonButton = buttonTransform.GetComponent<Button>();
        Text buttonText = buttonTransform.Find("Text").GetComponent<Text>();
        buttonText.text = "X";

        RectTransform labelTransform = buttonText.rectTransform;
        labelTransform = UnityEngine.Object.Instantiate(labelTransform, ourLabelWithXButtonPrefab.transform);
        labelTransform.name = "Text";
        labelTransform.anchorMax = new Vector2(1, 1);
        labelTransform.anchorMin = new Vector2(0, 0);
        labelTransform.offsetMax = new Vector2(-65, 0);
        labelTransform.offsetMin = new Vector2(5, -10);
        Text labelText = labelTransform.GetComponent<Text>();
        labelText.verticalOverflow = VerticalWrapMode.Overflow;

        UIDynamicLabelXButton uid = ourLabelWithXButtonPrefab.AddComponent<UIDynamicLabelXButton>();
        uid.label = labelText;
        uid.button = buttonButton;
      }
      {
        Transform t = ourCreateUIElement(ourLabelWithXButtonPrefab.transform, side == UIColumn.RIGHT);
        UIDynamicLabelXButton uid = t.gameObject.GetComponent<UIDynamicLabelXButton>();
        uid.label.text = label;
        uid.button.onClick.AddListener(callback);
        t.gameObject.SetActive(true);
        return uid;
      }
    }

    // Create an info textbox with scrolling disabled
    public static UIDynamicTextInfo CreateInfoTextNoScroll(UIColumn side, string text, float height)
    {
      if (ourTextInfoPrefab == null)
      {
        ourTextInfoPrefab = new GameObject("TextInfo");
        ourTextInfoPrefab.SetActive(false);
        RectTransform rt = ourTextInfoPrefab.AddComponent<RectTransform>();
        rt.anchorMax = new Vector2(0, 1);
        rt.anchorMin = new Vector2(0, 1);
        rt.offsetMax = new Vector2(535, -500);
        rt.offsetMin = new Vector2(10, -600);
        LayoutElement le = ourTextInfoPrefab.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.minHeight = 35;
        le.minWidth = 350;
        le.preferredHeight = 35;
        le.preferredWidth = 500;

        RectTransform backgroundTransform = script.manager.configurableScrollablePopupPrefab.transform.Find("Background") as RectTransform;
        backgroundTransform = UnityEngine.Object.Instantiate(backgroundTransform, ourTextInfoPrefab.transform);
        backgroundTransform.name = "Background";
        backgroundTransform.anchorMax = new Vector2(1, 1);
        backgroundTransform.anchorMin = new Vector2(0, 0);
        backgroundTransform.offsetMax = new Vector2(0, 0);
        backgroundTransform.offsetMin = new Vector2(0, -10);

        RectTransform labelTransform = script.manager.configurableScrollablePopupPrefab.transform.Find("Button/Text") as RectTransform; ;
        labelTransform = UnityEngine.Object.Instantiate(labelTransform, ourTextInfoPrefab.transform);
        labelTransform.name = "Text";
        labelTransform.anchorMax = new Vector2(1, 1);
        labelTransform.anchorMin = new Vector2(0, 0);
        labelTransform.offsetMax = new Vector2(-5, 0);
        labelTransform.offsetMin = new Vector2(5, 0);
        Text labelText = labelTransform.GetComponent<Text>();
        labelText.alignment = TextAnchor.UpperLeft;

        UIDynamicTextInfo uid = ourTextInfoPrefab.AddComponent<UIDynamicTextInfo>();
        uid.text = labelText;
        uid.layout = le;
        uid.background = backgroundTransform;
        uid.image = backgroundTransform.GetComponent<Image>();
      }
      {
        Transform t = ourCreateUIElement(ourTextInfoPrefab.transform, side == UIColumn.RIGHT);
        UIDynamicTextInfo uid = t.gameObject.GetComponent<UIDynamicTextInfo>();
        uid.text.text = text;
        uid.layout.minHeight = height;
        uid.layout.preferredHeight = height;
        t.gameObject.SetActive(true);
        return uid;
      }
    }

    // Create an info textbox with scrolling disabled with a specified number of lines
    public static UIDynamicTextInfo CreateInfoTextNoScroll(UIColumn side, string text, int lines)
    {
      UIDynamicTextInfo uid = CreateInfoTextNoScroll(side, text, lines * 32f);
      return uid;
    }

    // Create an info textbox with scrolling disabled with a specified number of lines
    public static UIDynamicTextInfo CreateInfoTextNoBackground(UIColumn side, string text, float height = 40f)
    {
      UIDynamicTextInfo uid = CreateInfoTextNoScroll(side, text, height);
      uid.image.color = UIColor.TRANSPARENT;
      return uid;
    }

    public static UIDynamicTextInfo CreateHeaderText(UIColumn side, string text, float size = 45f)
    {
      UIDynamicTextInfo uid = CreateInfoTextNoBackground(side, $"<size={size * 0.85f}><b>{text}</b></size>", size);
      return uid;
    }

    // Create two buttons on one line
    public static UIDynamicTwinButton CreateTwinButton(UIColumn side, string leftLabel, UnityAction leftCallback, string rightLabel, UnityAction rightCallback)
    {
      if (ourTwinButtonPrefab == null)
      {
        ourTwinButtonPrefab = new GameObject("TwinButton");
        ourTwinButtonPrefab.SetActive(false);
        RectTransform rt = ourTwinButtonPrefab.AddComponent<RectTransform>();
        rt.anchorMax = new Vector2(0, 1);
        rt.anchorMin = new Vector2(0, 1);
        rt.offsetMax = new Vector2(535, -500);
        rt.offsetMin = new Vector2(10, -600);
        LayoutElement le = ourTwinButtonPrefab.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.minHeight = 50;
        le.minWidth = 350;
        le.preferredHeight = 50;
        le.preferredWidth = 500;

        RectTransform buttonTransform = script.manager.configurableScrollablePopupPrefab.transform.Find("Button") as RectTransform;
        buttonTransform = UnityEngine.Object.Instantiate(buttonTransform, ourTwinButtonPrefab.transform);
        buttonTransform.name = "ButtonLeft";
        buttonTransform.anchorMax = new Vector2(0.5f, 1.0f);
        buttonTransform.anchorMin = new Vector2(0.0f, 0.0f);
        buttonTransform.offsetMax = new Vector2(-3, 0);
        buttonTransform.offsetMin = new Vector2(0, 0);
        Button buttonLeft = buttonTransform.GetComponent<Button>();
        Text labelLeft = buttonTransform.Find("Text").GetComponent<Text>();

        buttonTransform = UnityEngine.Object.Instantiate(buttonTransform, ourTwinButtonPrefab.transform);
        buttonTransform.name = "ButtonRight";
        buttonTransform.anchorMax = new Vector2(1.0f, 1.0f);
        buttonTransform.anchorMin = new Vector2(0.5f, 0.0f);
        buttonTransform.offsetMax = new Vector2(0, 0);
        buttonTransform.offsetMin = new Vector2(3, 0);
        Button buttonRight = buttonTransform.GetComponent<Button>();
        Text labelRight = buttonTransform.Find("Text").GetComponent<Text>();

        UIDynamicTwinButton uid = ourTwinButtonPrefab.AddComponent<UIDynamicTwinButton>();
        uid.labelLeft = labelLeft;
        uid.labelRight = labelRight;
        uid.buttonLeft = buttonLeft;
        uid.buttonRight = buttonRight;
      }
      {
        Transform t = ourCreateUIElement(ourTwinButtonPrefab.transform, side == UIColumn.RIGHT);
        UIDynamicTwinButton uid = t.GetComponent<UIDynamicTwinButton>();
        uid.labelLeft.text = leftLabel;
        uid.labelRight.text = rightLabel;
        uid.buttonLeft.onClick.AddListener(leftCallback);
        uid.buttonRight.onClick.AddListener(rightCallback);
        t.gameObject.SetActive(true);
        return uid;
      }
    }

    // Create a list of buttons that spans both columns
    public static UIDynamicTabBar CreateTabBar(UIColumn anchorSide, string[] menuItems, TabClickCallback callback, int tabsPerRow = 6)
    {
      GameObject tabBarPrefab;
      {
        float rowHeight = 50f;
        float rowWidth = 1060f;
        float spacing = 5f;

        int numRows = Mathf.CeilToInt((float)menuItems.Length / (float)tabsPerRow);
        float totalHeight = numRows * (rowHeight + spacing);
        float tabWidth = ((rowWidth + spacing) / tabsPerRow) - spacing;
        float tabHeight = rowHeight;

        tabBarPrefab = new GameObject("TabBar");
        LayoutElement le = tabBarPrefab.AddComponent<LayoutElement>();
        le.minHeight = totalHeight;
        le.minWidth = rowWidth;

        UIDynamicTabBar uid = tabBarPrefab.AddComponent<UIDynamicTabBar>();
        RectTransform buttonPrefab = script.manager.configurableScrollablePopupPrefab.transform.Find("Button") as RectTransform;

        for (int i = 0; i < menuItems.Length; i++)
        {
          int col = i % tabsPerRow;
          int row = i / tabsPerRow;

          float xOffset = col * (tabWidth + spacing);
          if (anchorSide == UIColumn.RIGHT) {
            xOffset -= rowWidth / 2 + 9f;
          }
          float yOffset = -row * (rowHeight + spacing);

          RectTransform tabRect = InstantiateBasicRectTransform(
            "TabButton",
            buttonPrefab,
            tabBarPrefab.transform,
            new float[] { 0f, 1f, 0f, 1f },
            new float[] { xOffset, yOffset - tabHeight, xOffset + tabWidth, yOffset }
          );

          Button tabButton = tabRect.GetComponent<Button>();
          uid.buttons.Add(tabButton);
          Text tabText = tabRect.Find("Text").GetComponent<Text>();
          tabText.text = menuItems[i];
        }
      }
      {
        Transform t = ourCreateUIElement(tabBarPrefab.transform, anchorSide == UIColumn.RIGHT);
        UIDynamicTabBar uid = t.gameObject.GetComponent<UIDynamicTabBar>();
        for (int i = 0; i < uid.buttons.Count; i++)
        {
          string item = menuItems[i];
          uid.buttons[i].onClick.AddListener(
            () => { callback(item); }
          );
        }
        UnityEngine.Object.Destroy(tabBarPrefab);
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
          else if (uid is UIDynamicUtils)
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

    private static RectTransform InstantiateBasicRectTransform(string prefabName, RectTransform original, Transform parent, float[] anchorCoords, float[] offsetCoords)
    {
      RectTransform rt = UnityEngine.Object.Instantiate(original, parent);
      rt.name = prefabName;
      rt.anchorMin = new Vector2(anchorCoords[0], anchorCoords[1]);
      rt.anchorMax = new Vector2(anchorCoords[2], anchorCoords[3]);
      rt.offsetMin = new Vector2(offsetCoords[0], offsetCoords[1]);
      rt.offsetMax = new Vector2(offsetCoords[2], offsetCoords[3]);
      return rt;
    }

    private static void QueueLoadTexture(string url, TextureSettings settings, TextureSetCallback callback)
    {
      if (ImageLoaderThreaded.singleton == null)
        return;
      if (string.IsNullOrEmpty(url))
        return;

      ImageLoaderThreaded.QueuedImage queuedImage = new ImageLoaderThreaded.QueuedImage();
      queuedImage.imgPath = url;
      queuedImage.forceReload = true;
      queuedImage.skipCache = true;
      queuedImage.compress = settings.compress;
      queuedImage.createMipMaps = settings.createMipMaps;
      queuedImage.isNormalMap = settings.isNormalMap;
      queuedImage.linear = settings.linearColor;
      queuedImage.createAlphaFromGrayscale = settings.createAlphaFromGrayscale;
      queuedImage.createNormalFromBump = settings.createNormalFromBump;
      queuedImage.bumpStrength = settings.bumpStrength;
      queuedImage.isThumbnail = false;
      queuedImage.fillBackground = false;
      queuedImage.invert = false;
      queuedImage.callback = (ImageLoaderThreaded.QueuedImage qi) =>
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
      };
      ImageLoaderThreaded.singleton.QueueImage(queuedImage);
    }

    private static MVRScript script;
    private static CreateUIElement ourCreateUIElement;
    private static GameObject ourLabelWithInputPrefab;
    private static GameObject ourLabelWithXButtonPrefab;
    private static GameObject ourTextInfoPrefab;
    private static GameObject ourTwinButtonPrefab;
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

    private const string IDAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"; 
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

    // Adjust slider max range to next power of 10 (1, 10, 100, 1000, ...) from slider value
    public static void AdjustSliderRange(JSONStorableFloat slider)
    {
      float m = Mathf.Log10(slider.val);
      m = Mathf.Max(Mathf.Ceil(m), 1);
      slider.max = Mathf.Pow(10, m);
    }

    // Adjust maxSlider value and max range after minSlider was changed to ensure minSlider <= maxSlider.
    public static void AdjustMaxSliderFromMin(float minValue, JSONStorableFloat maxSlider)
    {
      if (maxSlider.slider != null)
        maxSlider.max = maxSlider.slider.maxValue; // slider sometimes does not update the storable

      float v = Mathf.Max(minValue, maxSlider.val);
      float m = Mathf.Max(v, maxSlider.max);
      m = Mathf.Max(Mathf.Ceil(Mathf.Log10(m)), 1);
      maxSlider.max = Mathf.Pow(10, m);
      maxSlider.valNoCallback = v;
    }

    // Destroy a potentially null game object safely
    public static void SafeDestroy(ref GameObject go)
    {
      if (go != null)
      {
        UnityEngine.Object.Destroy(go);
        go = null;
      }
    }

    public static void LogTransform(string message, Transform t)
    {
      StringBuilder b = new StringBuilder();
      b.Append(message).Append("\n");
      LogTransformInternal(t, 0, b);
      SuperController.LogMessage(b.ToString());
    }

    private static void LogTransformInternal(Transform t, int indent, StringBuilder b)
    {
      b.Append(' ', indent * 4).Append(t.name).Append(" (active: ").Append(t.gameObject.activeSelf).Append(")\n");

      Component[] comps = t.GetComponents<Component>();
      if (comps.Length > 0)
      {
        b.Append(' ', indent * 4 + 2).Append("Components:\n");
        for (int i = 0; i < comps.Length; ++i)
        {
          Component c = comps[i];
          b.Append(' ', indent * 4 + 4).Append(c.GetType().FullName).Append("\n");

          if (c is RectTransform)
          {
            RectTransform rt = c as RectTransform;
            b.Append(' ', indent * 4 + 8).Append("anchoredPosition=").Append(rt.anchoredPosition).Append("\n");
            b.Append(' ', indent * 4 + 8).Append("anchorMax=").Append(rt.anchorMax).Append("\n");
            b.Append(' ', indent * 4 + 8).Append("anchorMin=").Append(rt.anchorMin).Append("\n");
            b.Append(' ', indent * 4 + 8).Append("offsetMax=").Append(rt.offsetMax).Append("\n");
            b.Append(' ', indent * 4 + 8).Append("offsetMin=").Append(rt.offsetMin).Append("\n");
            b.Append(' ', indent * 4 + 8).Append("pivot=").Append(rt.pivot).Append("\n");
            b.Append(' ', indent * 4 + 8).Append("rect=").Append(rt.rect).Append("\n");
          }
          else if (c is LayoutElement)
          {
            LayoutElement le = c as LayoutElement;
            b.Append(' ', indent * 4 + 8).Append("flexibleHeight=").Append(le.flexibleHeight).Append("\n");
            b.Append(' ', indent * 4 + 8).Append("flexibleWidth=").Append(le.flexibleWidth).Append("\n");
            b.Append(' ', indent * 4 + 8).Append("ignoreLayout=").Append(le.ignoreLayout).Append("\n");
            b.Append(' ', indent * 4 + 8).Append("layoutPriority=").Append(le.layoutPriority).Append("\n");
            b.Append(' ', indent * 4 + 8).Append("minHeight=").Append(le.minHeight).Append("\n");
            b.Append(' ', indent * 4 + 8).Append("minWidth=").Append(le.minWidth).Append("\n");
            b.Append(' ', indent * 4 + 8).Append("preferredHeight=").Append(le.preferredHeight).Append("\n");
            b.Append(' ', indent * 4 + 8).Append("preferredWidth=").Append(le.preferredWidth).Append("\n");
          }
          else if (c is Image)
          {
            Image img = c as Image;
            b.Append(' ', indent * 4 + 8).Append("mainTexture=").Append(img.mainTexture?.name).Append("\n");
            b.Append(' ', indent * 4 + 8).Append("sprite=").Append(img.sprite?.name).Append("\n");
            b.Append(' ', indent * 4 + 8).Append("color=").Append(img.color).Append("\n");
          }
        }
      }
      if (t.childCount > 0)
      {
        b.Append(' ', indent * 4 + 2).Append("Children:\n");
        for (int i = 0; i < t.childCount; ++i)
        {
          Transform c = t.GetChild(i);
          LogTransformInternal(c, indent + 1, b);
        }
      }
    }
  }

  // ================================================================================================== //
  // ========================================== HELPER TYPES ========================================== //
  // ================================================================================================== //
  public delegate Transform CreateUIElement(Transform prefab, bool rightSide);
  public delegate void EnumSetCallback<TEnum>(TEnum v);
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

  public class UIDynamicUtils : UIDynamic
  {
  }

  public class UIDynamicTextInput : UIDynamicUtils
  {
    public Text label;
    public InputField input;
  }

  public class UIDynamicLabelXButton : UIDynamicUtils
  {
    public Text label;
    public Button button;
  }

  public class UIDynamicTwinButton : UIDynamicUtils
  {
    public Text labelLeft;
    public Text labelRight;
    public Button buttonLeft;
    public Button buttonRight;
  }

  public class UIDynamicTextInfo : UIDynamicUtils
  {
    public Text text;
    public LayoutElement layout;
    public RectTransform background;
    public Image image;
  }

  public class UIDynamicTabBar : UIDynamicUtils
  {
    public List<Button> buttons = new List<Button>();
  }
}
