﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Febucci.HierarchyData
{

    // Copyright (c) 2020 Federico Bellucci - febucci.com
    // 
    // Permission is hereby granted, free of charge, to any person obtaining a copy of this software/algorithm and associated
    // documentation files (the "Software"), to use, copy, modify, merge or distribute copies of the Software, and to permit
    // persons to whom the Software is furnished to do so, subject to the following conditions:
    // 
    // - The Software, substantial portions, or any modified version be kept free of charge and cannot be sold commercially.
    // 
    // - The above copyright and this permission notice shall be included in all copies, substantial portions or modified
    // versions of the Software.
    // 
    // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
    // WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
    // COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
    // OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    // 
    // For any other use, please ask for permission by contacting the author.
    [InitializeOnLoad]
    public static class HierarchyDrawer
    {
        static HierarchyDrawer()
        {
            Initialize();
        }
        
        static class HierarchyRenderer
        {
            static private HierarchyData.TreeData.BranchGroup currentBranch;

            private static readonly HierarchyData.TreeData.BranchGroup fallbackGroup =
                new HierarchyData.TreeData.BranchGroup()
                {
                    overlayColor = new Color(1f, 0.44f, 0.97f, .04f),
                    colors = new[]
                    {
                        new Color(1f, 0.44f, 0.97f), new Color(0.56f, 0.44f, 1f), new Color(0.44f, 0.71f, 1f),
                        new Color(0.19f, 0.53f, 0.78f)
                    }
                };
            
            public static void SwitchBranchesColors(int hierarchyIndex)
            {
                int targetIndex = hierarchyIndex % data.tree.branches.Length;
                if (data.tree.branches.Length == 0 || data.tree.branches[targetIndex].colors.Length<=0)
                {
                    currentBranch = fallbackGroup;
                    return;
                }
                
                currentBranch = data.tree.branches[targetIndex];
            }
            
            private const float barWidth = 2;
            
            public static void DrawNestGroupOverlay(Rect originalRect)
            {
                if (currentBranch.overlayColor.a <= 0) return;
                
                originalRect = new Rect(32, originalRect.y, originalRect.width + (originalRect.x-32), originalRect.height);
                EditorGUI.DrawRect(originalRect, currentBranch.overlayColor);
            }

            public static float GetStartX(Rect originalRect, int nestLevel)
            {
                return 37 + (originalRect.height-2) * nestLevel;
                //return originalRect.x                //aligned start position (9 is the magic number here)
                //    - originalRect.height * 2 //GameObject icon offset
                //    + 9
                //    - nestLevel * (originalRect.height - 2);       
            }

            static Color GetNestColor(int nestLevel)
            {
                return currentBranch.colors[nestLevel % currentBranch.colors.Length];
            }

            public static void DrawVerticalLineFrom(Rect originalRect, int nestLevel)
            {
                DrawHalfVerticalLineFrom(originalRect, true, nestLevel);   
                DrawHalfVerticalLineFrom(originalRect, false, nestLevel);   
            }

            public static void DrawHalfVerticalLineFrom(Rect originalRect, bool startsOnTop, int nestLevel)
            {
                if(currentBranch.colors.Length<=0) return;

                DrawHalfVerticalLineFrom(originalRect, startsOnTop, nestLevel, GetNestColor(nestLevel));
            }
            
            public static void DrawHalfVerticalLineFrom(Rect originalRect, bool startsOnTop, int nestLevel, Color color)
            {
                //Vertical rect, starts from the very left and then proceeds to te right
                EditorGUI.DrawRect(
                    new Rect(
                        GetStartX(originalRect, nestLevel), 
                        startsOnTop ? originalRect.y : (originalRect.y + originalRect.height/2f), 
                        barWidth, 
                        originalRect.height/2f
                        ), 
                    color
                    );
            }

            public static void DrawHorizontalLineFrom(Rect originalRect, int nestLevel, bool hasChilds, bool isLastChildInNestingLevel)
            {
                if (currentBranch.colors.Length <= 0) return;

                //Vertical rect, starts from the very left and then proceeds to te right
                EditorGUI.DrawRect(
                    new Rect(
                        GetStartX(originalRect, nestLevel) + (isLastChildInNestingLevel ? 0 : barWidth),
                        originalRect.y + originalRect.height / 2f,
                        originalRect.height + (hasChilds ? -5 : 2),
                        //originalRect.height - 5, 
                        barWidth
                        ),
                    GetNestColor(nestLevel)
                    );
            }
        }

        #region Types

        [Serializable]
        struct InstanceInfo
        {
            /// <summary>
            /// Contains the indexes for each icon to draw, eg. draw(iconTextures[iconIndexes[0]]);
            /// </summary>
            public List<int> iconIndexes;
            public bool isSeparator;
            public string goName;
            public int prefabInstanceID;
            public bool isGoActive;

            public bool isLastElement;
            public bool isLastChildInNestingLevel;
            public bool hasChilds;
            public bool topParentHasChild;
            
            public int nestingGroup;
            public int nestingLevel;

            public int parentId;
        }

        #endregion

        private static bool initialized = false;
        private static HierarchyData data;
        private static int firstInstanceID = 0;
        private static List<int> iconsPositions = new List<int>();
        private static Dictionary<int, InstanceInfo> sceneGameObjects = new Dictionary<int, InstanceInfo>();
        private static Dictionary<int, Color> prefabColors = new Dictionary<int, Color>();

        #region Menu Items

        #region Internal

        [MenuItem("Tools/Febucci/Custom Hierarchy/Initialize or Create", priority = 1)]
        public static void InitializeOrCreate()
        {
            if (Load()) //file exists
            {
                Initialize();
                SelectData();
            }
            else
            {
                //file does not exist; asks the user if they want to create it
                if (EditorUtility.DisplayDialog("Custom Hierarchy", "Do you want to create an Hierarchy Icon Data?", "Yes", "No"))
                {
                    CreateAsset();
                }
                else
                {
                    Debug.Log("Hierarchy Icon: Data creation was canceled.");
                }
            }
        }

        static bool SelectData()
        {
            var loaded = Load();
            if (loaded != null)
            {
                //EditorUtility.FocusProjectWindow();
                Selection.activeObject = loaded;

                return true;
            }

            return false;
        }

        #endregion

        #region Blog

        [MenuItem("Tools/Febucci/📝 Blog", priority = 50)]
        static void m_OpenBlog()
        {
            Application.OpenURL("https://www.febucci.com/blog");
        }

        [MenuItem("Tools/Febucci/🐦 Twitter", priority = 51)]
        static void m_OpenTwitter()
        {
            Application.OpenURL("https://twitter.com/febucci");
        }

        #endregion

        #endregion

        #region Initialization/Helpers

        private const string fileName = "HierarchyData";
        static HierarchyData Load()
        {
            var result = EditorGUIUtility.Load($"Febucci/{fileName}.asset") as HierarchyData;
            if (result != null)
                return result;

            var guids = UnityEditor.AssetDatabase.FindAssets("t:" + nameof(HierarchyData));
            if (guids.Length == 0)
                return null;

            return AssetDatabase.LoadAssetAtPath<HierarchyData>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        /// <summary>
        /// Creates the Hierarchy Asset File
        /// </summary>
        static void CreateAsset()
        {
            if (Load())
            {
                Debug.LogWarning("HierarchyIcons: Data already exists, won't create a new one.");
                return;
            }
            
            //Creates folder
            if(!AssetDatabase.IsValidFolder("Assets/Editor Default Resources"))
                AssetDatabase.CreateFolder("Assets", "Editor Default Resources");

            string path = "Assets/Editor Default Resources/Febucci";
            if (!AssetDatabase.IsValidFolder("Assets/Editor Default Resources/Febucci"))
            {
                string guid = AssetDatabase.CreateFolder("Assets/Editor Default Resources", "Febucci");
                path = AssetDatabase.GUIDToAssetPath(guid);
            }

            try
            {
                //Creates asset
                var asset = ScriptableObject.CreateInstance<HierarchyData>();
                AssetDatabase.CreateAsset(asset, path + $"/{fileName}.asset");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            AssetDatabase.SaveAssets();

            Initialize();
            //Focusses new asset
            SelectData();
            Debug.Log("Hierarchy Data asset was created in the 'Assets/Editor Default Resources'Febucci' folder.");
        }

        /// <summary>
        /// Initializes the script at the beginning. 
        /// </summary>
        public static void Initialize()
        {
            #region Unregisters previous events

            if (initialized)
            {
                //Prevents registering events multiple times
                EditorApplication.hierarchyWindowItemOnGUI -= DrawCore;
                EditorApplication.hierarchyChanged -= RetrieveDataFromScene;
            }

            #endregion

            initialized = false;
            data = Load();

            if (!data) return; //no data found

            initialized = true;

            if (data.enabled)
            {

                #region Registers events

                EditorApplication.hierarchyWindowItemOnGUI += DrawCore;
                EditorApplication.hierarchyChanged += RetrieveDataFromScene;

                #endregion

                RetrieveDataFromScene();
                
                prefabColors.Clear();
                foreach (var prefab in data.prefabsData.prefabs)
                {
                    if (prefab.color.a<=0) continue;
                    if (!prefab.gameObject) continue;
                    
                    int instanceID = prefab.gameObject.GetInstanceID();
                    if(prefabColors.ContainsKey(instanceID)) continue;
                    
                    prefabColors.Add(instanceID, prefab.color);
                }
            }

            EditorApplication.RepaintHierarchyWindow();
        }

        #endregion

        /// <summary>
        /// Updates the list of objects to draw, icons etc.
        /// </summary>
        static void RetrieveDataFromScene()
        {
            if (!data.updateInPlayMode && Application.isPlaying) //temp. fix for performance reasons while in play mode
                return;

            sceneGameObjects.Clear();
            iconsPositions.Clear();

            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                var prefabContentsRoot = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
                
                AnalyzeGoWithChildren(
                    go: prefabContentsRoot,
                    nestingLevel: -1,
                    prefabContentsRoot.transform.childCount > 0,
                    nestingGroup: 0,
                    isLastChild: true);

                firstInstanceID = prefabContentsRoot.GetInstanceID();

                return;
            }

            GameObject[] sceneRoots;
            Scene tempScene;
            firstInstanceID = -1;
            
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                tempScene = SceneManager.GetSceneAt(i);
                if (tempScene.isLoaded)
                {
                    sceneRoots = tempScene.GetRootGameObjects();
                    //Analyzes all scene's gameObjects
                    for (int j = 0; j < sceneRoots.Length; j++)
                    {
                        AnalyzeGoWithChildren(
                            go: sceneRoots[j],
                            nestingLevel: 0,
                            sceneRoots[j].transform.childCount > 0,
                            nestingGroup: j,
                            isLastChild: j == sceneRoots.Length - 1,
                            -1
                            );
                    }

                    if (firstInstanceID == -1 && sceneRoots.Length > 0) firstInstanceID = sceneRoots[0].GetInstanceID();
                }
            }
        }

        static void AnalyzeGoWithChildren(GameObject go, int nestingLevel, bool topParentHasChild, int nestingGroup, bool isLastChild, int parentId)
        {
            int instanceID = go.GetInstanceID();

            if (!sceneGameObjects.ContainsKey(instanceID)) //processes the gameobject only if it wasn't processed already
            {
                InstanceInfo newInfo = new InstanceInfo();
                newInfo.iconIndexes = new List<int>();
                newInfo.isLastElement = isLastChild && go.transform.childCount == 0;
                newInfo.isLastChildInNestingLevel = isLastChild;
                newInfo.nestingLevel = nestingLevel;
                newInfo.nestingGroup = nestingGroup;
                newInfo.hasChilds = go.transform.childCount > 0;
                newInfo.isGoActive = go.activeInHierarchy;
                newInfo.topParentHasChild = topParentHasChild;
                newInfo.goName = go.name;
                newInfo.parentId = parentId;

                if (data.prefabsData.enabled)
                {
                    var prefab = PrefabUtility.GetCorrespondingObjectFromSource(go);
                    if (prefab)
                    {
                        newInfo.prefabInstanceID = prefab.GetInstanceID();
                    }
                }

                newInfo.isSeparator = String.Compare(go.tag, "EditorOnly", StringComparison.Ordinal) == 0 //gameobject has EditorOnly tag
                                      && (!string.IsNullOrEmpty(go.name) && !string.IsNullOrEmpty(data.separator.startString) && go.name.StartsWith(data.separator.startString)); //and also starts with '>'

                if (data.icons.enabled && data.icons.pairs!=null && data.icons.pairs.Length>0)
                {

                    #region Components Information (icons)

                    Type classReferenceType; //todo opt
                    Type componentType;

                    foreach (var c in go.GetComponents<Component>())
                    {
                        if(!c) continue;

                        componentType = c.GetType();

                        for (int elementIndex = 0; elementIndex < data.icons.pairs.Length; elementIndex++)
                        {
                            if (!data.icons.pairs[elementIndex].iconToDraw) continue;

                            //Class inherithance
                            foreach (var classReference in data.icons.pairs[elementIndex].targetClasses)
                            {
                                if(!classReference) continue;
                                
                                classReferenceType = classReference.GetClass();
                                
                                if(!classReferenceType.IsClass) continue;

                                //class ineriths 
                                if (componentType.IsAssignableFrom(classReferenceType) || componentType.IsSubclassOf(classReferenceType))
                                {
                                    //Adds the icon index to the "positions" list, to draw all of them in order later [if enabled] 
                                    if (!iconsPositions.Contains(elementIndex)) iconsPositions.Add(elementIndex);

                                    //Adds the icon index to draw, only if it's not present already
                                    if (!newInfo.iconIndexes.Contains(elementIndex))
                                        newInfo.iconIndexes.Add(elementIndex);


                                    break;
                                }
                            }
                        }
                    }

                    newInfo.iconIndexes.Sort();

                    #endregion
                }

                //Adds element to the array
                sceneGameObjects.Add(instanceID, newInfo);
            }

            #region Analyzes Childrens

            int childCount = go.transform.childCount;
            for (int j = 0; j < childCount; j++)
            {
                AnalyzeGoWithChildren(
                    go.transform.GetChild(j).gameObject,
                    nestingLevel + 1,
                    topParentHasChild,
                    nestingGroup,
                    j == childCount - 1,
                    instanceID
                    );
            }

            #endregion
        }

        #region Drawing

        private static bool temp_alternatingDrawed;
        private static int temp_iconsDrawedCount;
        private static InstanceInfo currentItem;
        private static bool drawedPrefabOverlay;

        
        static void DrawCore(int instanceID, Rect selectionRect)
        {
            //skips early if item is not registered or not valid
            if (!sceneGameObjects.ContainsKey(instanceID)) return;

            currentItem = sceneGameObjects[instanceID];
            temp_iconsDrawedCount = -1;
            GameObject go = null;

            if (instanceID == firstInstanceID)
            {
                temp_alternatingDrawed = currentItem.nestingGroup %2 == 0;
            }

            #region Draw Activation Toggle
            if (data.drawActivationToggle)
            {
                temp_iconsDrawedCount++;

                go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

                if (go == null)
                    return;

                var r = new Rect(selectionRect.xMax - 16 * (temp_iconsDrawedCount + 1) - 2, selectionRect.yMin, 16, 16);

                var wasActive = go.activeSelf;
                var isActive = GUI.Toggle(r, wasActive, "");
                if (wasActive != isActive)
                {
                    go.SetActive(isActive);
                    if (EditorApplication.isPlaying == false)
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
                        EditorUtility.SetDirty(go);
                    }
                }
            }
            #endregion

            #region Draw Alternating BG

            if (data.alternatingBackground.enabled)
            {
                if (temp_alternatingDrawed)
                {
                    EditorGUI.DrawRect(selectionRect, data.alternatingBackground.color);
                    temp_alternatingDrawed = false;
                }
                else
                {
                    temp_alternatingDrawed = true;
                }
            }
            

            #endregion

            #region DrawingPrefabsBackground

            drawedPrefabOverlay = false;
            if (data.prefabsData.enabled && prefabColors.Count>0)
            {
                if (prefabColors.ContainsKey(currentItem.prefabInstanceID))
                {
                    EditorGUI.DrawRect(selectionRect, prefabColors[currentItem.prefabInstanceID]);
                    drawedPrefabOverlay = true;
                }
            }


            #endregion

            #region Drawing Separators Functionality

            Action drawSeparator = () =>
            {

                //EditorOnly objects are only removed from build if they're not childrens
                if (data.separator.enabled && data.separator.color.a > 0
                                           && currentItem.isSeparator && currentItem.nestingLevel == 0)
                {
                    if (data.separator.fullWidth)
                    {
                        var startX = HierarchyRenderer.GetStartX(selectionRect, 0) - 6;
                        var fullWidthRect = new Rect(
                            startX,
                            selectionRect.y,
                            selectionRect.width + (selectionRect.x - startX),
                            selectionRect.height
                        );
                        EditorGUI.DrawRect(fullWidthRect, data.separator.color);
                    }
                    else
                    {
                        EditorGUI.DrawRect(selectionRect, data.separator.color);
                    }
                }

            };

            #endregion

            #region Drawing Separators Under Tree

            if (data.separator.drawUnderTree)
            {
                drawSeparator();
            }

            #endregion
            
            #region Drawing Tree

            if (data.tree.enabled
                && currentItem.nestingLevel >= 0)
            {
                if (selectionRect.x >= 60) //prevents drawing when the hierarchy search mode is enabled 
                {
                    HierarchyRenderer.SwitchBranchesColors(currentItem.nestingGroup);

                    //Group
                    if ((data.tree.drawOverlayOnColoredPrefabs || !drawedPrefabOverlay) && currentItem.topParentHasChild)
                    {
                        HierarchyRenderer.DrawNestGroupOverlay(selectionRect);
                    }
                    

                    if (currentItem.nestingLevel == 0 && !currentItem.hasChilds)
                    {
                        HierarchyRenderer.DrawHalfVerticalLineFrom(selectionRect, true, 0, data.tree.baseLevelColor);
                        HierarchyRenderer.DrawHalfVerticalLineFrom(selectionRect, false, 0, data.tree.baseLevelColor);
                    }
                    else
                    {
                        var nestedItem = currentItem;
                        //Draws a vertical line for each previous nesting level
                        for (int i = currentItem.nestingLevel; i >= 0; i--)
                        {
                            if (data.tree.drawBranchTails)
                            {
                                HierarchyRenderer.DrawVerticalLineFrom(selectionRect, i);
                            }
                            else
                            {
                                if (!nestedItem.isLastChildInNestingLevel)
                                {
                                    HierarchyRenderer.DrawVerticalLineFrom(selectionRect, i);
                                }
                                else if (i == currentItem.nestingLevel)
                                {
                                    HierarchyRenderer.DrawHalfVerticalLineFrom(selectionRect, true, i);
                                }

                                if (!sceneGameObjects.ContainsKey(nestedItem.parentId))
                                {
                                    break;
                                }
                                nestedItem = sceneGameObjects[nestedItem.parentId];
                            }
                        }

                        HierarchyRenderer.DrawHorizontalLineFrom(
                            selectionRect, currentItem.nestingLevel, currentItem.hasChilds, currentItem.isLastChildInNestingLevel
                            );

                    }

                }

                //draws a super small divider between different groups
                if (currentItem.nestingLevel == 0 && data.tree.dividerHeigth > 0)
                {
                    Rect boldGroupRect = new Rect(
                        32, selectionRect.y - data.tree.dividerHeigth / 2f,
                        selectionRect.width + (selectionRect.x - 32),
                        data.tree.dividerHeigth
                        );
                    EditorGUI.DrawRect(boldGroupRect, Color.black * .3f);
                }
                

            }

            #endregion

            #region Drawing Separators Over Tree

            if (!data.separator.drawUnderTree)
            {
                drawSeparator();
            }

            #endregion

            #region Drawing Icon

            if (data.icons.enabled)
            {
                #region Local Methods

                // Draws each component icon
                void DrawIcon(int textureIndex)
                {
                    //---Icon Alignment---
                    if (data.icons.aligned)
                    {
                        //Aligns icon based on texture's position on the array
                        int CalculateIconPosition()
                        {
                            for (var i = 0; i < iconsPositions.Count; i++)
                                if (iconsPositions[i] == textureIndex)
                                    return i;

                            return 0;
                        }

                        temp_iconsDrawedCount = CalculateIconPosition();
                    }
                    else
                    {
                        temp_iconsDrawedCount++;
                    }
                    //---

                    GUI.DrawTexture(
                        new Rect(selectionRect.xMax - 16 * (temp_iconsDrawedCount + 1) - 2, selectionRect.yMin, 16, 16),
                        data.icons.pairs[textureIndex].iconToDraw
                    );
                }
                
                // Draws each gameobject icon
                void DrawGameObjectIcon()
                {
                    //if enabled
                    var content = EditorGUIUtility.ObjectContent(
                        go ?? EditorUtility.InstanceIDToObject(instanceID),
                        null);

                    if (content.image && !string.IsNullOrEmpty(content.image.name))
                        if (content.image.name != "d_GameObject Icon" && content.image.name != "d_Prefab Icon")
                        {
                            temp_iconsDrawedCount++;
                            GUI.DrawTexture(
                                new Rect(selectionRect.xMax - 16 * (temp_iconsDrawedCount + 1) - 2,
                                    selectionRect.yMin,
                                    16,
                                    16),
                                content.image);
                        }
                }

                #endregion

                //Draws the gameobject icon, if present & enabled
                if (data.icons.drawGameObjectIcon)
                {
                    //whether to draw only when no pair set
                    if (data.icons.drawGOIconOnlyIfNoPairSet)
                    {
                        //no pair draw
                        if (currentItem.iconIndexes.Count.Equals(0))
                        {
                            DrawGameObjectIcon();
                        }
                        
                        //else dont draw
                    }
                    else
                    {
                        DrawGameObjectIcon();
                    }
                }

                //Draws the component icons, if present
                foreach (var index in currentItem.iconIndexes) DrawIcon(index);
            }

            #endregion
        }

        #endregion
    }
}
