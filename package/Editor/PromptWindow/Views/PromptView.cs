using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Scenario
{
    public partial class PromptWindowUI
    {
        private void RenderPromptSection()
        {
            GUILayout.BeginHorizontal();
            {
                CustomStyle.Label("Prompt", width: 64, alignment: TextAnchor.MiddleCenter);
                HandlePositiveInputField();
                GUIContent plusPrompt = new GUIContent(EditorGUIUtility.IconContent("d_Toolbar Plus").image);
                if (GUILayout.Button(plusPrompt, GUILayout.Width(25), GUILayout.Height(25)))
                {
                    PromptBuilderWindow.isFromNegativePrompt = false;
                    PromptBuilderWindow.ShowWindow(PromptRecv, tags);
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

                    while (currentTagIndex < tags.Count)
                    {
                        EditorGUILayout.BeginHorizontal();

                        for (int i = 0; i < tagsPerRow && currentTagIndex < tags.Count; i++)
                        {
                            string tag = tags[currentTagIndex];
                            string displayTag = TruncateTag(tag);

                            GUIContent tagContent = new GUIContent(displayTag, tag);
                            Rect tagRect = GUILayoutUtility.GetRect(tagContent, customTagStyle);

                            bool isActiveTag = currentTagIndex == dragFromIndex;
                            GUIStyle tagStyle = isActiveTag ? new GUIStyle(customTagStyle) { normal = { background = MakeTex(2, 2, isActiveTag ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.8f, 0.8f, 0.8f)) } } : customTagStyle;

                            Rect xRect = new Rect(tagRect.xMax - 20, tagRect.y, 20, tagRect.height);

                            if (Event.current.type == EventType.MouseDown)
                            {
                                if (Event.current.button == 0 && Event.current.clickCount == 2 && tagRect.Contains(Event.current.mousePosition))
                                {
                                    int plusCount = tag.Count(c => c == '+');
                                    if (plusCount < 3)
                                    {
                                        tags[currentTagIndex] += "+";
                                    }
                                }
                                else if (Event.current.button == 1 && tagRect.Contains(Event.current.mousePosition))
                                {
                                    if (tag.EndsWith("+"))
                                    {
                                        tags[currentTagIndex] = tag.Remove(tag.LastIndexOf('+'));
                                    }
                                }
                            }

                            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && tagRect.Contains(Event.current.mousePosition))
                            {
                                if (!xRect.Contains(Event.current.mousePosition))
                                {
                                    dragFromIndex = currentTagIndex;
                                    dragStartPos = Event.current.mousePosition;
                                    Event.current.Use();
                                }
                            }

                            if (dragFromIndex >= 0 && Event.current.type == EventType.MouseDrag)
                            {
                                int newIndex = GetNewIndex(Event.current.mousePosition);
                                if (newIndex != -1 && newIndex != dragFromIndex && newIndex < tags.Count)
                                {
                                    string tempTag = tags[dragFromIndex];
                                    tags.RemoveAt(dragFromIndex);
                                    tags.Insert(newIndex, tempTag);
                                    dragFromIndex = newIndex;
                                }
                            }

                            if (Event.current.type == EventType.MouseUp)
                            {
                                dragFromIndex = -1;
                            }

                            EditorGUI.LabelField(tagRect, tagContent, tagStyle);

                            if (GUI.Button(xRect, "x"))
                            {
                                tags.RemoveAt(currentTagIndex);
                            }
                            else
                            {
                                currentTagIndex++;
                            }

                            tagRects.Add(tagRect);
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
        }

        private void HandlePositiveInputField()
        {
            GUI.SetNextControlName("inputTextField");
            inputText = EditorGUILayout.TextField(inputText, GUILayout.ExpandWidth(true), GUILayout.Height(25), GUILayout.MinWidth(400));

            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return &&
                GUI.GetNameOfFocusedControl() == "inputTextField")
            {
                if (!string.IsNullOrWhiteSpace(inputText))
                {
                    string descriptorName = inputText.Trim();
                    tags.Add(descriptorName);
                    inputText = "";
                    Event.current.Use();
                }
                else
                {
                    EditorGUI.FocusTextInControl("inputTextField");
                    Event.current.Use();
                }
            }
        }
    }
}