﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor
{
    class ProjectAuditorWindow : EditorWindow
    {
        private ProjectReport m_ProjectReport;
        private IssueTable m_IssueTable;

        private bool m_EnableCPU = true;
        private bool m_EnableGPU = true;
        private bool m_EnableMemory = true;
        private bool m_EnableBuildSize = true;
        private bool m_EnableLoadTimes = true;
        private bool m_EnablePackages = false;
        private bool m_EnableResolvedItems = false;
        private bool m_EnableAPICalls = true;
        private bool m_EnableProjectSettings = true;

        public static GUIStyle Toolbar;
        public static readonly GUIContent analyzeButton = new GUIContent("Analyze Project", "Analyze Project.\nAnalyze Project and list all issues found.");

        private void OnEnable()
        {
            m_ProjectReport = new ProjectReport();
        }

        private void OnGUI()
        {
            Toolbar = "Toolbar";

            DrawToolbar();

            if (m_IssueTable != null)
            {
                Rect r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
                m_IssueTable.OnGUI(r);

                DrawDetails();
            }
        }

        bool ShouldDisplay(ProjectIssue issue)
        {
            string category = issue.category;
            if (!m_EnableAPICalls && category.Contains("API Call"))
                return false;
            if (!m_EnableProjectSettings && category.Contains("ProjectSettings"))
                return false;

            string url = issue.url;
            if (!m_EnablePackages && category.Contains("API Call") &&
                (url.Contains("Library/PackageCache/") || url.Contains("Resources/PackageManager/BuiltInPackages/")))
                return false;

            if (!m_EnableResolvedItems && issue.resolved == true)
                return false;

            string area = issue.def.area;
            if (m_EnableCPU && area.Contains("CPU"))
                return true;
            if (m_EnableGPU && area.Contains("GPU"))
                return true;
            if (m_EnableMemory && area.Contains("Memory"))
                return true;
            if (m_EnableBuildSize && area.Contains("Build Size"))
                return true;
            if (m_EnableLoadTimes && area.Contains("Load Times"))
                return true;

            return false;
        }

        private void Analyze()
        {
            m_ProjectReport.Create();
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            MultiColumnHeaderState.Column[] columns = new MultiColumnHeaderState.Column[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Resolved?", "Resolved?"),
                    width = 80,
                    minWidth = 80,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Category", "Category"),
                    width = 100,
                    minWidth = 100,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Area", "Area"),
                    width = 100,
                    minWidth = 100,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Description", "Description"),
                    width = 300,
                    minWidth = 100,
                    autoResize = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Location", "Location"),
                    width = 900,
                    minWidth = 400,
                    autoResize = true
                },        };

            var filteredList = m_ProjectReport.m_ProjectIssues.Where(x => ShouldDisplay(x));

            m_IssueTable = new IssueTable(new TreeViewState(),
                new MultiColumnHeader(new MultiColumnHeaderState(columns)), filteredList.ToArray());
        }

        private void Reload()
        {
            m_IssueTable = null;
            m_ProjectReport = new ProjectReport();
        }

        private void Serialize()
        {
            m_ProjectReport.WriteToFile();
        }

        private void DrawDetails()
        {
            if (m_IssueTable.HasSelection())
            {
                EditorStyles.textField.wordWrap = true;

                //            var index = m_IssueTable.GetSelection()[0];
                //            var issue = m_ProjectReport.m_ProjectIssues[index];

                var displayIndex = m_IssueTable.GetSelection()[0];
                int listIndex = 0;
                int i = 0;

                for (; i < m_ProjectReport.m_ProjectIssues.Count; ++i)
                {
                    if (ShouldDisplay(m_ProjectReport.m_ProjectIssues[i]))
                    {
                        if (listIndex == displayIndex)
                        {
                            break;
                        }
                        ++listIndex;
                    }
                }

                var issue = m_ProjectReport.m_ProjectIssues[i];

                // TODO: use an Issue interface, to define how to display different categories
                string text = string.Empty;

                text = $"Issue: {issue.def.problem}";
                EditorGUILayout.TextArea(text, GUILayout.Height(100)/*, GUILayout.ExpandHeight(true)*/ );

                text = $"Recommendation: {issue.def.solution}";
                EditorGUILayout.TextArea(text, GUILayout.Height(100)/*, GUILayout.ExpandHeight(true)*/);
                EditorStyles.textField.wordWrap = false;

            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(Toolbar);

            GUIStyle buttonStyle = GUI.skin.button;
            if (GUILayout.Button("Analyze", buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                Analyze();
            if (GUILayout.Button("Reload DB", buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                Reload();
            if (GUILayout.Button("Serialize", buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Width(80)))
                Serialize();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(Toolbar);
            GUILayout.Label("Filter By:", GUILayout.ExpandWidth(true), GUILayout.Width(80));

            EditorGUI.BeginChangeCheck();

            m_EnableMemory = EditorGUILayout.ToggleLeft("Memory", m_EnableMemory, GUILayout.Width(100));
            m_EnableCPU = EditorGUILayout.ToggleLeft("CPU", m_EnableCPU, GUILayout.Width(100));
            m_EnableGPU = EditorGUILayout.ToggleLeft("GPU", m_EnableGPU, GUILayout.Width(100));
            m_EnableBuildSize = EditorGUILayout.ToggleLeft("Build Size", m_EnableBuildSize, GUILayout.Width(100));
            m_EnableLoadTimes = EditorGUILayout.ToggleLeft("Load Times", m_EnableLoadTimes, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(Toolbar);
            GUILayout.Label("", GUILayout.ExpandWidth(true), GUILayout.Width(80));
            m_EnablePackages = EditorGUILayout.ToggleLeft("Packages", m_EnablePackages, GUILayout.Width(100));
            m_EnableResolvedItems = EditorGUILayout.ToggleLeft("Resolved Items", m_EnableResolvedItems, GUILayout.Width(100));
            m_EnableAPICalls = EditorGUILayout.ToggleLeft("API Calls", m_EnableAPICalls, GUILayout.Width(100));
            m_EnableProjectSettings = EditorGUILayout.ToggleLeft("Project Settings", m_EnableProjectSettings, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                RefreshDisplay();
            }
        }

        [MenuItem("Window/Analysis/Project Auditor")]
        public static ProjectAuditorWindow ShowWindow()
        {
            var wnd = GetWindow(typeof(ProjectAuditorWindow)) as ProjectAuditorWindow;
            if (wnd != null)
            {
                wnd.titleContent = EditorGUIUtility.TrTextContent("Project Auditor");
            }
            return wnd;
        }
    }
}