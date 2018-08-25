﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.﻿

using Microsoft.MixedReality.Toolkit.Inspectors.Data;
using Microsoft.MixedReality.Toolkit.Inspectors.Utilities;
using Microsoft.MixedReality.Toolkit.Internal.Definitions.Devices;
using Microsoft.MixedReality.Toolkit.Internal.Definitions.InputSystem;
using Microsoft.MixedReality.Toolkit.Internal.Definitions.Utilities;
using Microsoft.MixedReality.Toolkit.Internal.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Inspectors
{
    public class ControllerPopupWindow : EditorWindow
    {
        private const string EditorWindowOptionsPath = "/MixedRealityToolkit/_Core/Inspectors/Data/EditorWindowOptions.json";
        private const float InputActionLabelWidth = 128f;

        /// <summary>
        /// Used to enable editing the input axis label positions on controllers
        /// </summary>
        private static readonly bool EnableWysiwyg = false;

        private static readonly GUIContent InteractionAddButtonContent = new GUIContent("+ Add a New Interaction Mapping");
        private static readonly GUIContent InteractionMinusButtonContent = new GUIContent("-", "Remove Interaction Mapping");
        private static readonly GUIContent AxisTypeContent = new GUIContent("Axis Type", "The axis type of the button, e.g. Analogue, Digital, etc.");
        private static readonly GUIContent ControllerInputTypeContent = new GUIContent("Input Type", "The primary action of the input as defined by the controller SDK.");
        private static readonly GUIContent ActionContent = new GUIContent("Action", "Action to be raised to the Input Manager when the input data has changed.");
        private static readonly GUIContent KeyCodeContent = new GUIContent("KeyCode", "Unity Input KeyCode id to listen for.");
        private static readonly GUIContent XAxisContent = new GUIContent("X Axis", "Horizontal Axis to listen for.");
        private static readonly GUIContent YAxisContent = new GUIContent("Y Axis", "Vertical Axis to listen for.");
        private static readonly GUIContent InvertContent = new GUIContent("Invert", "Should an Axis be inverted?");
        private static readonly GUIContent[] InvertAxisContent =
        {
            new GUIContent("None"),
            new GUIContent("Invert X"),
            new GUIContent("Invert Y"),
            new GUIContent("Both")
        };

        private static readonly int[] InvertAxisValues = { 0, 1, 2, 3 };

        private static readonly Vector2 InputActionLabelPosition = new Vector2(256f, 0f);
        private static readonly Vector2 InputActionDropdownPosition = new Vector2(88f, 0f);
        private static readonly Vector2 InputActionFlipTogglePosition = new Vector2(-24f, 0f);
        private static readonly Vector2 HorizontalSpace = new Vector2(8f, 0f);

        private static readonly Rect ControllerRectPosition = new Rect(new Vector2(128f, 0f), new Vector2(512f, 512f));

        private static ControllerPopupWindow window;
        private static ControllerInputActionOptions controllerInputActionOptions;

        private static GUIContent[] axisLabels;
        private static int[] actionIds;
        private static GUIContent[] actionLabels;
        private static int[] rawActionIds;
        private static GUIContent[] rawActionLabels;
        private static int[] digitalActionIds;
        private static GUIContent[] digitalActionLabels;
        private static int[] singleAxisActionIds;
        private static GUIContent[] singleAxisActionLabels;
        private static int[] dualAxisActionIds;
        private static GUIContent[] dualAxisActionLabels;
        private static int[] threeDofPositionActionIds;
        private static GUIContent[] threeDofPositionActionLabels;
        private static int[] threeDofRotationActionIds;
        private static GUIContent[] threeDofRotationActionLabels;
        private static int[] sixDofActionIds;
        private static GUIContent[] sixDofActionLabels;
        private static bool[] isMouseInRects;

        private static bool editInputActionPositions;

        private static float defaultLabelWidth;
        private static float defaultFieldWidth;

        private static Vector2 horizontalScrollPosition;

        private SerializedProperty currentInteractionList;

        private bool isCustomController;

        private ControllerPopupWindow thisWindow;

        private Handedness currentHandedness;
        private SupportedControllerType currentControllerType;

        private Vector2 mouseDragOffset;
        private GUIStyle flippedLabelStyle;
        private Texture2D currentControllerTexture;
        private ControllerInputActionOption currentControllerOption;

        private void OnFocus()
        {
            isCustomController = false;

            switch (currentControllerType)
            {
                case SupportedControllerType.None:
                    break;
                case SupportedControllerType.GenericOpenVR:
                    isCustomController = true;
                    break;
                case SupportedControllerType.ViveWand:
                    if (currentHandedness == Handedness.Left)
                    {
                        currentControllerTexture = EditorGUIUtility.isProSkin ? ControllerMappingLibrary.ViveWandControllerLeftWhite : ControllerMappingLibrary.ViveWandControllerLeftBlack;
                    }
                    else if (currentHandedness == Handedness.Right)
                    {
                        currentControllerTexture = EditorGUIUtility.isProSkin ? ControllerMappingLibrary.ViveWandControllerRightWhite : ControllerMappingLibrary.ViveWandControllerRightBlack;
                    }
                    break;
                case SupportedControllerType.ViveKnuckles:
                    break;
                case SupportedControllerType.OculusTouch:
                    if (currentHandedness == Handedness.Left)
                    {
                        currentControllerTexture = EditorGUIUtility.isProSkin ? ControllerMappingLibrary.TouchControllerLeftWhite : ControllerMappingLibrary.TouchControllerLeftBlack;
                    }
                    else if (currentHandedness == Handedness.Right)
                    {
                        currentControllerTexture = EditorGUIUtility.isProSkin ? ControllerMappingLibrary.TouchControllerRightWhite : ControllerMappingLibrary.TouchControllerRightBlack;
                    }
                    break;
                case SupportedControllerType.OculusRemote:
                    currentControllerTexture = EditorGUIUtility.isProSkin ? ControllerMappingLibrary.OculusRemoteControllerWhite : ControllerMappingLibrary.OculusRemoteControllerBlack;
                    break;
                case SupportedControllerType.WindowsMixedReality:
                    if (currentHandedness == Handedness.Left)
                    {
                        currentControllerTexture = EditorGUIUtility.isProSkin ? ControllerMappingLibrary.WmrControllerLeftWhite : ControllerMappingLibrary.WmrControllerLeftBlack;
                    }
                    else if (currentHandedness == Handedness.Right)
                    {
                        currentControllerTexture = EditorGUIUtility.isProSkin ? ControllerMappingLibrary.WmrControllerRightWhite : ControllerMappingLibrary.WmrControllerRightBlack;
                    }
                    break;
                case SupportedControllerType.GenericUnityDevice:
                    isCustomController = true;
                    break;
                case SupportedControllerType.XboxController:
                    currentControllerTexture = EditorGUIUtility.isProSkin ? ControllerMappingLibrary.XboxControllerWhite : ControllerMappingLibrary.XboxControllerBlack;
                    break;
            }

            #region Interaction Constraint Setup

            actionIds = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Select(action => (int)action.Id)
                .Prepend(0).ToArray();

            axisLabels = ControllerMappingLibrary.UnityInputManagerAxes
                .Select(axis => new GUIContent(axis.Name))
                .Prepend(new GUIContent("None")).ToArray();

            actionIds = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Where(inputAction => inputAction.AxisConstraint == AxisType.None)
                .Select(action => (int)action.Id)
                .Prepend(0).ToArray();

            actionLabels = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Where(inputAction => inputAction.AxisConstraint == AxisType.None)
                .Select(inputAction => new GUIContent(inputAction.Description))
                .Prepend(new GUIContent("None")).ToArray();

            rawActionIds = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Where(inputAction => inputAction.AxisConstraint == AxisType.Raw)
                .Select(action => (int)action.Id)
                .Prepend(0).ToArray();

            rawActionLabels = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                 .Where(inputAction => inputAction.AxisConstraint == AxisType.Raw)
                 .Select(inputAction => new GUIContent(inputAction.Description))
                 .Prepend(new GUIContent("None")).ToArray();

            digitalActionIds = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Where(inputAction => inputAction.AxisConstraint == AxisType.Digital)
                .Select(action => (int)action.Id)
                .Prepend(0).ToArray();

            digitalActionLabels = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Where(inputAction => inputAction.AxisConstraint == AxisType.Digital)
                .Select(inputAction => new GUIContent(inputAction.Description))
                .Prepend(new GUIContent("None")).ToArray();

            singleAxisActionIds = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Where(inputAction => inputAction.AxisConstraint == AxisType.SingleAxis)
                .Select(action => (int)action.Id)
                .Prepend(0).ToArray();

            singleAxisActionLabels = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Where(inputAction => inputAction.AxisConstraint == AxisType.SingleAxis)
                .Select(inputAction => new GUIContent(inputAction.Description))
                .Prepend(new GUIContent("None")).ToArray();

            dualAxisActionIds = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Where(inputAction => inputAction.AxisConstraint == AxisType.DualAxis)
                .Select(action => (int)action.Id).Prepend(0).ToArray();

            dualAxisActionLabels = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Where(inputAction => inputAction.AxisConstraint == AxisType.DualAxis)
                .Select(inputAction => new GUIContent(inputAction.Description))
                .Prepend(new GUIContent("None")).ToArray();

            threeDofPositionActionIds = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Where(inputAction => inputAction.AxisConstraint == AxisType.ThreeDofPosition)
                .Select(action => (int)action.Id)
                .Prepend(0).ToArray();

            threeDofPositionActionLabels = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Where(inputAction => inputAction.AxisConstraint == AxisType.ThreeDofPosition)
                .Select(inputAction => new GUIContent(inputAction.Description))
                .Prepend(new GUIContent("None")).ToArray();

            threeDofRotationActionIds = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Where(inputAction => inputAction.AxisConstraint == AxisType.ThreeDofRotation)
                .Select(action => (int)action.Id)
                .Prepend(0).ToArray();

            threeDofRotationActionLabels = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Where(inputAction => inputAction.AxisConstraint == AxisType.ThreeDofRotation)
                .Select(inputAction => new GUIContent(inputAction.Description))
                .Prepend(new GUIContent("None")).ToArray();

            sixDofActionIds = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Where(inputAction => inputAction.AxisConstraint == AxisType.SixDof)
                .Select(action => (int)action.Id)
                .Prepend(0).ToArray();

            sixDofActionLabels = MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions
                .Where(inputAction => inputAction.AxisConstraint == AxisType.SixDof)
                .Select(inputAction => new GUIContent(inputAction.Description))
                .Prepend(new GUIContent("None")).ToArray();

            #endregion  Interaction Constraint Setup
        }

        public static void Show(SupportedControllerType controllerType, SerializedProperty interactionsList, Handedness handedness = Handedness.None)
        {
            window = (ControllerPopupWindow)GetWindow(typeof(ControllerPopupWindow));
            window.Close();
            window = (ControllerPopupWindow)CreateInstance(typeof(ControllerPopupWindow));
            window.thisWindow = window;
            var handednessTitleText = handedness != Handedness.None ? $"{handedness} Hand " : string.Empty;
            window.titleContent = new GUIContent($"{controllerType} {handednessTitleText}Input Action Assignment");
            window.currentControllerType = controllerType;
            window.currentHandedness = handedness;
            window.currentInteractionList = interactionsList;
            isMouseInRects = new bool[interactionsList.arraySize];

            if (!File.Exists($"{Application.dataPath}{EditorWindowOptionsPath}"))
            {
                var empty = new ControllerInputActionOptions
                {
                    Controllers = new List<ControllerInputActionOption>
                    {
                        new ControllerInputActionOption
                        {
                            Controller = SupportedControllerType.None,
                            Handedness = Handedness.None,
                            InputLabelPositions = new[] {new Vector2(0, 0)},
                            IsLabelFlipped = new []{false}
                        }
                    }
                };

                File.WriteAllText($"{Application.dataPath}{EditorWindowOptionsPath}", JsonUtility.ToJson(empty));
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }
            else
            {
                controllerInputActionOptions = JsonUtility.FromJson<ControllerInputActionOptions>(File.ReadAllText($"{Application.dataPath}{EditorWindowOptionsPath}"));

                if (controllerInputActionOptions.Controllers.Any(option => option.Controller == controllerType && option.Handedness == handedness))
                {
                    window.currentControllerOption = controllerInputActionOptions.Controllers.FirstOrDefault(option => option.Controller == controllerType && option.Handedness == handedness);

                    if (window.currentControllerOption != null && window.currentControllerOption.IsLabelFlipped == null)
                    {
                        window.currentControllerOption.IsLabelFlipped = new bool[interactionsList.arraySize];
                    }
                }
            }

            window.ShowUtility();
            var windowSize = new Vector2(controllerType == SupportedControllerType.GenericOpenVR || controllerType == SupportedControllerType.GenericUnityDevice ? 896f : 768f, 512f);
            window.maxSize = windowSize;
            window.minSize = windowSize;
            window.CenterOnMainWin();
            defaultLabelWidth = EditorGUIUtility.labelWidth;
            defaultFieldWidth = EditorGUIUtility.fieldWidth;
        }

        private void Update()
        {
            if (editInputActionPositions)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            if (flippedLabelStyle == null)
            {
                flippedLabelStyle = new GUIStyle("Label")
                {
                    alignment = TextAnchor.UpperRight,
                    stretchWidth = true
                };
            }

            if (!isCustomController)
            {
                GUILayout.BeginHorizontal();
                GUI.DrawTexture(ControllerRectPosition, currentControllerTexture);
                GUILayout.EndHorizontal();
            }

            try
            {
                RenderInteractionList(currentInteractionList, isCustomController);
            }
            catch (Exception)
            {
                thisWindow.Close();
            }
        }

        private void RenderInteractionList(SerializedProperty interactionList, bool useCustomInteractionMapping)
        {
            if (interactionList == null) { throw new Exception(); }

            bool noInteractions = interactionList.arraySize == 0;

            if (currentControllerOption != null && (currentControllerOption.IsLabelFlipped == null || currentControllerOption.IsLabelFlipped.Length != interactionList.arraySize))
            {
                currentControllerOption.IsLabelFlipped = new bool[interactionList.arraySize];
            }

            GUILayout.BeginVertical();

            if (useCustomInteractionMapping)
            {
                horizontalScrollPosition = EditorGUILayout.BeginScrollView(horizontalScrollPosition, false, false, GUILayout.ExpandWidth(true), GUILayout.ExpandWidth(true));
            }

            if (useCustomInteractionMapping)
            {
                if (GUILayout.Button(InteractionAddButtonContent))
                {
                    interactionList.arraySize += 1;
                    var interaction = interactionList.GetArrayElementAtIndex(interactionList.arraySize - 1);
                    var axisType = interaction.FindPropertyRelative("axisType");
                    axisType.enumValueIndex = 0;
                    var inputType = interaction.FindPropertyRelative("inputType");
                    inputType.enumValueIndex = 0;
                    var action = interaction.FindPropertyRelative("inputAction");
                    var actionId = action.FindPropertyRelative("id");
                    var actionDescription = action.FindPropertyRelative("description");
                    actionDescription.stringValue = "None";
                    actionId.intValue = 0;
                }

                if (noInteractions)
                {
                    EditorGUILayout.HelpBox("Create an Interaction Mapping.", MessageType.Warning);
                    EditorGUILayout.EndScrollView();
                    GUILayout.EndVertical();
                    return;
                }
            }
            else if (EnableWysiwyg)
            {
                EditorGUI.BeginChangeCheck();
                editInputActionPositions = EditorGUILayout.Toggle("Edit Input Action Positions", editInputActionPositions);

                if (EditorGUI.EndChangeCheck())
                {
                    if (!editInputActionPositions)
                    {
                        File.WriteAllText($"{Application.dataPath}{EditorWindowOptionsPath}", JsonUtility.ToJson(controllerInputActionOptions));
                    }
                    else
                    {
                        if (!controllerInputActionOptions.Controllers.Any(option => option.Controller == currentControllerType && option.Handedness == currentHandedness))
                        {
                            currentControllerOption = new ControllerInputActionOption
                            {
                                Controller = currentControllerType,
                                Handedness = currentHandedness,
                                InputLabelPositions = new Vector2[currentInteractionList.arraySize],
                                IsLabelFlipped = new bool[currentInteractionList.arraySize]
                            };

                            controllerInputActionOptions.Controllers.Add(currentControllerOption);
                            isMouseInRects = new bool[currentInteractionList.arraySize];

                            if (controllerInputActionOptions.Controllers.Any(option => option.Controller == SupportedControllerType.None))
                            {
                                controllerInputActionOptions.Controllers.Remove(
                                    controllerInputActionOptions.Controllers.Find(option =>
                                        option.Controller == SupportedControllerType.None));
                            }

                            File.WriteAllText($"{Application.dataPath}{EditorWindowOptionsPath}", JsonUtility.ToJson(controllerInputActionOptions));
                        }
                    }
                }
            }

            GUILayout.BeginHorizontal();

            if (useCustomInteractionMapping)
            {
                EditorGUILayout.LabelField("Id", GUILayout.Width(32f));
                EditorGUIUtility.labelWidth = 24f;
                EditorGUIUtility.fieldWidth = 24f;
                EditorGUILayout.LabelField(ControllerInputTypeContent, GUILayout.Width(InputActionLabelWidth));
                EditorGUILayout.LabelField(AxisTypeContent, GUILayout.Width(InputActionLabelWidth));
                EditorGUILayout.LabelField(ActionContent, GUILayout.Width(InputActionLabelWidth));
                EditorGUILayout.LabelField(KeyCodeContent, GUILayout.Width(InputActionLabelWidth));
                EditorGUILayout.LabelField(XAxisContent, GUILayout.Width(InputActionLabelWidth));
                EditorGUILayout.LabelField(YAxisContent, GUILayout.Width(InputActionLabelWidth));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(string.Empty, GUILayout.Width(24f));

                EditorGUIUtility.labelWidth = defaultLabelWidth;
                EditorGUIUtility.fieldWidth = defaultFieldWidth;
            }

            GUILayout.EndHorizontal();

            for (int i = 0; i < interactionList.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                SerializedProperty interaction = interactionList.GetArrayElementAtIndex(i);

                if (useCustomInteractionMapping)
                {
                    EditorGUILayout.LabelField($"{i + 1}", GUILayout.Width(32f));
                    var inputType = interaction.FindPropertyRelative("inputType");
                    EditorGUILayout.PropertyField(inputType, GUIContent.none, GUILayout.Width(InputActionLabelWidth));
                    var axisType = interaction.FindPropertyRelative("axisType");
                    EditorGUILayout.PropertyField(axisType, GUIContent.none, GUILayout.Width(InputActionLabelWidth));
                    var invertXAxis = interaction.FindPropertyRelative("invertXAxis");
                    var invertYAxis = interaction.FindPropertyRelative("invertYAxis");
                    var interactionAxisConstraint = interaction.FindPropertyRelative("axisType");

                    var action = interaction.FindPropertyRelative("inputAction");
                    var actionId = action.FindPropertyRelative("id");
                    var actionDescription = action.FindPropertyRelative("description");
                    var actionConstraint = action.FindPropertyRelative("axisConstraint");

                    GUIContent[] labels;
                    int[] ids;

                    switch ((AxisType)interactionAxisConstraint.intValue)
                    {
                        default:
                        case AxisType.None:
                            labels = actionLabels;
                            ids = actionIds;
                            break;
                        case AxisType.Raw:
                            labels = rawActionLabels;
                            ids = rawActionIds;
                            break;
                        case AxisType.Digital:
                            labels = digitalActionLabels;
                            ids = digitalActionIds;
                            break;
                        case AxisType.SingleAxis:
                            labels = singleAxisActionLabels;
                            ids = singleAxisActionIds;
                            break;
                        case AxisType.DualAxis:
                            labels = dualAxisActionLabels;
                            ids = dualAxisActionIds;
                            break;
                        case AxisType.ThreeDofPosition:
                            labels = threeDofPositionActionLabels;
                            ids = threeDofPositionActionIds;
                            break;
                        case AxisType.ThreeDofRotation:
                            labels = threeDofRotationActionLabels;
                            ids = threeDofRotationActionIds;
                            break;
                        case AxisType.SixDof:
                            labels = sixDofActionLabels;
                            ids = sixDofActionIds;
                            break;
                    }

                    EditorGUI.BeginChangeCheck();
                    actionId.intValue = EditorGUILayout.IntPopup(GUIContent.none, actionId.intValue, labels, ids, GUILayout.Width(InputActionLabelWidth));

                    if (EditorGUI.EndChangeCheck())
                    {
                        var inputAction = actionId.intValue == 0 ? MixedRealityInputAction.None : MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions[actionId.intValue - 1];
                        actionDescription.stringValue = inputAction.Description;
                        actionConstraint.enumValueIndex = (int)inputAction.AxisConstraint;
                    }

                    if ((AxisType)axisType.intValue == AxisType.Digital)
                    {
                        var keyCode = interaction.FindPropertyRelative("keyCode");
                        EditorGUILayout.PropertyField(keyCode, GUIContent.none, GUILayout.Width(InputActionLabelWidth));
                    }
                    else
                    {
                        if ((AxisType)axisType.intValue == AxisType.DualAxis)
                        {
                            EditorGUIUtility.labelWidth = InputActionLabelWidth * 0.5f;
                            EditorGUIUtility.fieldWidth = InputActionLabelWidth * 0.5f;

                            int currentAxisSetting = 0;

                            if (invertXAxis.boolValue)
                            {
                                currentAxisSetting += 1;
                            }

                            if (invertYAxis.boolValue)
                            {
                                currentAxisSetting += 2;
                            }

                            EditorGUI.BeginChangeCheck();
                            currentAxisSetting = EditorGUILayout.IntPopup(InvertContent, currentAxisSetting, InvertAxisContent, InvertAxisValues, GUILayout.Width(InputActionLabelWidth));

                            if (EditorGUI.EndChangeCheck())
                            {
                                switch (currentAxisSetting)
                                {
                                    case 0:
                                        invertXAxis.boolValue = false;
                                        invertYAxis.boolValue = false;
                                        break;
                                    case 1:
                                        invertXAxis.boolValue = true;
                                        invertYAxis.boolValue = false;
                                        break;
                                    case 2:
                                        invertXAxis.boolValue = false;
                                        invertYAxis.boolValue = true;
                                        break;
                                    case 3:
                                        invertXAxis.boolValue = true;
                                        invertYAxis.boolValue = true;
                                        break;
                                }
                            }

                            EditorGUIUtility.labelWidth = defaultLabelWidth;
                            EditorGUIUtility.fieldWidth = defaultFieldWidth;
                        }
                        else
                        {
                            EditorGUILayout.LabelField(GUIContent.none, GUILayout.Width(InputActionLabelWidth));
                        }
                    }

                    if ((AxisType)axisType.intValue == AxisType.SingleAxis ||
                        (AxisType)axisType.intValue == AxisType.DualAxis)
                    {
                        var axisCodeX = interaction.FindPropertyRelative("axisCodeX");
                        RenderAxisPopup(axisCodeX, InputActionLabelWidth);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(GUIContent.none, GUILayout.Width(InputActionLabelWidth));
                    }

                    if ((AxisType)axisType.intValue == AxisType.DualAxis)
                    {
                        var axisCodeY = interaction.FindPropertyRelative("axisCodeY");
                        RenderAxisPopup(axisCodeY, InputActionLabelWidth);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(GUIContent.none, GUILayout.Width(InputActionLabelWidth));
                    }

                    if (GUILayout.Button(InteractionMinusButtonContent, EditorStyles.miniButtonRight, GUILayout.ExpandWidth(true)))
                    {
                        interactionList.DeleteArrayElementAtIndex(i);
                    }
                }
                else
                {
                    var interactionDescription = interaction.FindPropertyRelative("description");
                    var interactionAxisConstraint = interaction.FindPropertyRelative("axisType");
                    var action = interaction.FindPropertyRelative("inputAction");
                    var actionId = action.FindPropertyRelative("id");
                    var actionDescription = action.FindPropertyRelative("description");
                    var actionConstraint = action.FindPropertyRelative("axisConstraint");

                    GUIContent[] labels;
                    int[] ids;

                    switch ((AxisType)interactionAxisConstraint.intValue)
                    {
                        default:
                        case AxisType.None:
                            labels = actionLabels;
                            ids = actionIds;
                            break;
                        case AxisType.Raw:
                            labels = rawActionLabels;
                            ids = rawActionIds;
                            break;
                        case AxisType.Digital:
                            labels = digitalActionLabels;
                            ids = digitalActionIds;
                            break;
                        case AxisType.SingleAxis:
                            labels = singleAxisActionLabels;
                            ids = singleAxisActionIds;
                            break;
                        case AxisType.DualAxis:
                            labels = dualAxisActionLabels;
                            ids = dualAxisActionIds;
                            break;
                        case AxisType.ThreeDofPosition:
                            labels = threeDofPositionActionLabels;
                            ids = threeDofPositionActionIds;
                            break;
                        case AxisType.ThreeDofRotation:
                            labels = threeDofRotationActionLabels;
                            ids = threeDofRotationActionIds;
                            break;
                        case AxisType.SixDof:
                            labels = sixDofActionLabels;
                            ids = sixDofActionIds;
                            break;
                    }

                    EditorGUI.BeginChangeCheck();

                    if (currentControllerOption == null)
                    {
                        actionId.intValue = EditorGUILayout.IntPopup(GUIContent.none, actionId.intValue, labels, ids, GUILayout.Width(80f));
                        EditorGUILayout.LabelField(interactionDescription.stringValue, GUILayout.ExpandWidth(true));
                    }
                    else
                    {
                        var rectPosition = currentControllerOption.InputLabelPositions[i];
                        var rectSize = InputActionLabelPosition + InputActionDropdownPosition + new Vector2(currentControllerOption.IsLabelFlipped[i] ? 0f : 8f, EditorGUIUtility.singleLineHeight);
                        GUI.Box(new Rect(rectPosition, rectSize), GUIContent.none, EditorGUIUtility.isProSkin ? "ObjectPickerBackground" : "ObjectPickerResultsEven");
                        var offset = currentControllerOption.IsLabelFlipped[i] ? InputActionLabelPosition : Vector2.zero;
                        var popupRect = new Rect(rectPosition + offset, new Vector2(InputActionDropdownPosition.x, EditorGUIUtility.singleLineHeight));

                        actionId.intValue = EditorGUI.IntPopup(popupRect, actionId.intValue, labels, ids);
                        offset = currentControllerOption.IsLabelFlipped[i] ? Vector2.zero : InputActionDropdownPosition;
                        var labelRect = new Rect(rectPosition + offset, new Vector2(InputActionLabelPosition.x, EditorGUIUtility.singleLineHeight));
                        EditorGUI.LabelField(labelRect, interactionDescription.stringValue, currentControllerOption.IsLabelFlipped[i] ? flippedLabelStyle : EditorStyles.label);

                        if (editInputActionPositions)
                        {
                            offset = currentControllerOption.IsLabelFlipped[i] ? InputActionLabelPosition + InputActionDropdownPosition + HorizontalSpace : InputActionFlipTogglePosition;
                            var toggleRect = new Rect(rectPosition + offset, new Vector2(-InputActionFlipTogglePosition.x, EditorGUIUtility.singleLineHeight));

                            EditorGUI.BeginChangeCheck();
                            currentControllerOption.IsLabelFlipped[i] = EditorGUI.Toggle(toggleRect, currentControllerOption.IsLabelFlipped[i]);

                            if (EditorGUI.EndChangeCheck())
                            {
                                if (currentControllerOption.IsLabelFlipped[i])
                                {
                                    currentControllerOption.InputLabelPositions[i] -= InputActionLabelPosition;
                                }
                                else
                                {
                                    currentControllerOption.InputLabelPositions[i] += InputActionLabelPosition;
                                }
                            }

                            if (!isMouseInRects.Any(value => value) || isMouseInRects[i])
                            {
                                if (Event.current.type == EventType.MouseDrag && labelRect.Contains(Event.current.mousePosition) && !isMouseInRects[i])
                                {
                                    isMouseInRects[i] = true;
                                    mouseDragOffset = Event.current.mousePosition - currentControllerOption.InputLabelPositions[i];
                                }
                                else if (Event.current.type == EventType.Repaint && isMouseInRects[i])
                                {
                                    currentControllerOption.InputLabelPositions[i] = Event.current.mousePosition - mouseDragOffset;
                                }
                                else if (Event.current.type == EventType.DragUpdated && isMouseInRects[i])
                                {
                                    currentControllerOption.InputLabelPositions[i] = Event.current.mousePosition - mouseDragOffset;
                                }
                                else if (Event.current.type == EventType.MouseUp && isMouseInRects[i])
                                {
                                    currentControllerOption.InputLabelPositions[i] = Event.current.mousePosition - mouseDragOffset;
                                    mouseDragOffset = Vector2.zero;
                                    isMouseInRects[i] = false;
                                }
                            }
                        }
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        MixedRealityInputAction inputAction = actionId.intValue == 0 ?
                            MixedRealityInputAction.None :
                            MixedRealityManager.Instance.ActiveProfile.InputActionsProfile.InputActions[actionId.intValue - 1];
                        actionId.intValue = (int)inputAction.Id;
                        actionDescription.stringValue = inputAction.Description;
                        actionConstraint.enumValueIndex = (int)inputAction.AxisConstraint;
                        interactionList.serializedObject.ApplyModifiedProperties();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            if (useCustomInteractionMapping)
            {
                EditorGUILayout.EndScrollView();
                interactionList.serializedObject.ApplyModifiedProperties();
            }

            GUILayout.EndVertical();
        }

        private static void RenderAxisPopup(SerializedProperty axisCode, float customLabelWidth)
        {
            var axisId = -1;

            for (int j = 0; j < ControllerMappingLibrary.UnityInputManagerAxes.Length; j++)
            {
                if (ControllerMappingLibrary.UnityInputManagerAxes[j].Name == axisCode.stringValue)
                {
                    axisId = j + 1;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            axisId = EditorGUILayout.IntPopup(GUIContent.none, axisId, axisLabels, null, GUILayout.Width(customLabelWidth));

            if (EditorGUI.EndChangeCheck())
            {
                if (axisId == 0)
                {
                    axisCode.stringValue = string.Empty;
                    axisCode.serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    for (int j = 0; j < ControllerMappingLibrary.UnityInputManagerAxes.Length; j++)
                    {
                        if (axisId - 1 == j)
                        {
                            axisCode.stringValue = ControllerMappingLibrary.UnityInputManagerAxes[j].Name;
                            axisCode.serializedObject.ApplyModifiedProperties();
                            break;
                        }
                    }
                }
            }
        }
    }
}
