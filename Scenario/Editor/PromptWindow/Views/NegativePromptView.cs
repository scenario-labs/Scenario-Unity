using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public partial class PromptWindowUI
{
    private void RenderNegativePromptSection()
    {
        GUILayout.BeginHorizontal();
        {
            CustomStyle.Label("- Prompt", width: 64, alignment: TextAnchor.MiddleCenter);
            HandleNegativeInputField();
            GUIContent plusNegativePrompt = new GUIContent(EditorGUIUtility.IconContent("d_Toolbar Plus").image);
            if (GUILayout.Button(plusNegativePrompt, GUILayout.Width(25), GUILayout.Height(25)))
            {
                PromptBuilderWindow.isFromNegativePrompt = true;
                PromptBuilderWindow.ShowWindow(NegativePromptRecv, negativeTags);
            }
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Height(50));
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUIStyle customTagStyle = new GUIStyle(EditorStyles.label)
                {
                    fixedHeight = 25,
                    margin = new RectOffset(0, 5, 0, 5)
                };

                float availableWidth = EditorGUIUtility.currentViewWidth - 20;
                int tagsPerRow = Mathf.FloorToInt(availableWidth / 100);
                int currentTagIndex = 0;

                while (currentTagIndex < negativeTags.Count)
                {
                    EditorGUILayout.BeginHorizontal();

                    for (int i = 0; i < tagsPerRow && currentTagIndex < negativeTags.Count; i++)
                    {
                        string tag = negativeTags[currentTagIndex];
                        string displayTag = TruncateTag(tag);

                        GUIContent tagContent = new GUIContent(displayTag, tag);
                        Rect tagRect = GUILayoutUtility.GetRect(tagContent, customTagStyle);

                        bool isActiveTag = currentTagIndex == negativeDragFromIndex;
                        GUIStyle tagStyle = isActiveTag
                            ? new GUIStyle(customTagStyle)
                            {
                                normal =
                                {
                                    background = MakeTex(2, 2,
                                        isActiveTag ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.8f, 0.8f, 0.8f))
                                }
                            }
                            : customTagStyle;

                        Rect xRect = new Rect(tagRect.xMax - 20, tagRect.y, 20, tagRect.height);

                        if (Event.current.type == EventType.MouseDown)
                        {
                            if (Event.current.button == 0 && Event.current.clickCount == 2 &&
                                tagRect.Contains(Event.current.mousePosition))
                            {
                                int plusCount = tag.Split('+').Length - 1;
                                if (plusCount < 3)
                                {
                                    negativeTags[currentTagIndex] += "+";
                                }
                            }
                            else if (Event.current.button == 1 && tagRect.Contains(Event.current.mousePosition))
                            {
                                if (tag.EndsWith("+"))
                                {
                                    negativeTags[currentTagIndex] = tag.Remove(tag.LastIndexOf('+'));
                                }
                            }
                        }

                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                            tagRect.Contains(Event.current.mousePosition))
                        {
                            if (!xRect.Contains(Event.current.mousePosition))
                            {
                                negativeDragFromIndex = currentTagIndex;
                                dragStartPos = Event.current.mousePosition;
                                Event.current.Use();
                            }
                        }

                        if (negativeDragFromIndex >= 0 && Event.current.type == EventType.MouseDrag)
                        {
                            int newIndex = GetNewIndex(Event.current.mousePosition);
                            if (newIndex != -1 && newIndex != negativeDragFromIndex && newIndex < negativeTags.Count)
                            {
                                string tempTag = negativeTags[negativeDragFromIndex];
                                negativeTags.RemoveAt(negativeDragFromIndex);
                                negativeTags.Insert(newIndex, tempTag);
                                negativeDragFromIndex = newIndex;
                            }
                        }

                        if (Event.current.type == EventType.MouseUp)
                        {
                            negativeDragFromIndex = -1;
                        }

                        EditorGUI.LabelField(tagRect, tagContent, tagStyle);

                        if (GUI.Button(xRect, "x"))
                        {
                            negativeTags.RemoveAt(currentTagIndex);
                        }
                        else
                        {
                            currentTagIndex++;
                        }

                        negativeTagRects.Add(tagRect);
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();
    }

    private void HandleNegativeInputField()
    {
        GUI.SetNextControlName("negativeInputTextField");
        negativeInputText =
            EditorGUILayout.TextField(negativeInputText, GUILayout.ExpandWidth(true), GUILayout.Height(25), GUILayout.MinWidth(400));

        if (Event.current.isKey && Event.current.keyCode == KeyCode.Return &&
            GUI.GetNameOfFocusedControl() == "negativeInputTextField")
        {
            if (!string.IsNullOrWhiteSpace(negativeInputText))
            {
                string descriptorName = negativeInputText.Trim();
                negativeTags.Add(descriptorName);
                negativeInputText = "";
                Event.current.Use();
            }
            else
            {
                EditorGUI.FocusTextInControl("negativeInputTextField");
                Event.current.Use();
            }
        }
    }
}