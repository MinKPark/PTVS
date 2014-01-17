﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Diagnostics;
using System.Windows.Automation;
using Microsoft.TC.TestHostAdapters;
using Microsoft.VisualStudio.Shell.Interop;

namespace TestUtilities.UI {
    public class AutomationWrapper {
        private readonly AutomationElement _element;
        
        public AutomationWrapper(AutomationElement element) {
            Debug.Assert(element != null);
            _element = element;
        }

        /// <summary>
        /// Provides access to the underlying AutomationElement used for accessing the visual studio app.
        /// </summary>
        public AutomationElement Element {
            get {
                return _element;
            }
        }

        /// <summary>
        /// Clicks the child button with the specified automation ID.
        /// </summary>
        /// <param name="automationId"></param>
        public void ClickButtonByAutomationId(string automationId) {
            Invoke(FindByAutomationId(automationId));
        }

        
        /// <summary>
        /// Clicks the child button with the specified name.
        /// </summary>
        /// <param name="name"></param>
        public void ClickButtonByName(string name) {
            var button = FindByName(name);

            Invoke(button);
        }

        public AutomationElement FindByName(string name) {
            return FindFirstWithRetry(
                TreeScope.Descendants,
                new PropertyCondition(
                    AutomationElement.NameProperty,
                    name
                )
            );
        }

        private AutomationElement FindFirstWithRetry(TreeScope scope, Condition condition) {
            AutomationElement res = null;
            for (int i = 0; i < 20 && res == null; i++) {
                res = Element.FindFirst(scope, condition);
                if (res == null) {
                    Console.WriteLine("Failed to find element {0} on try {1}", condition, i);
                    if (i == 0) {
                        Console.WriteLine(new StackTrace(true).ToString());
                    }
                    System.Threading.Thread.Sleep(500);
                }
            }
            return res;
        }

        /// <summary>
        /// Finds the first descendent with the given automation ID.
        /// </summary>
        public AutomationElement FindByAutomationId(string automationId) {
            return FindFirstWithRetry(
                TreeScope.Descendants,
                new PropertyCondition(
                    AutomationElement.AutomationIdProperty,
                    automationId
                )
            );
        }

        /// <summary>
        /// Finds the child button with the specified name.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public AutomationElement FindButton(string text) {
            return FindFirstWithRetry(
                TreeScope.Descendants,
                new AndCondition(
                    new OrCondition(
                        new PropertyCondition(
                            AutomationElement.AutomationIdProperty,
                            text
                        ),
                        new PropertyCondition(
                            AutomationElement.NameProperty,
                            text
                        )
                    ),
                    new PropertyCondition(
                        AutomationElement.ClassNameProperty,
                        "Button"
                    )
                )
            );
        }
        
        /// <summary>
        /// Finds the first child element of a given control type.
        /// </summary>
        /// <param name="ctlType">The ControlType you wish to find</param>
        /// <returns></returns>
        public AutomationElement FindFirstByControlType(ControlType ctlType) {
            return FindFirstWithRetry(
                TreeScope.Descendants,
                new PropertyCondition(
                    AutomationElement.ControlTypeProperty,
                    ctlType
                )
            );
        }

        /// <summary>
        /// Finds the first child element of a given control type.
        /// </summary>
        /// <param name="ctlType">The ControlType you wish to find</param>
        /// <returns></returns>
        public AutomationElement FindFirstByNameAndAutomationId(string name, string automationId) {
            return FindFirstWithRetry(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(
                        AutomationElement.AutomationIdProperty,
                        automationId
                    ),
                    new PropertyCondition(
                        AutomationElement.NameProperty,
                        name
                    )
                )
           );
        }

        /// <summary>
        /// Finds the first child element of a given control type.
        /// </summary>
        /// <param name="ctlType">The ControlType you wish to find</param>
        /// <returns></returns>
        public AutomationElement FindFirstByControlType(string name, ControlType ctlType) {
            return FindFirstWithRetry(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(
                        AutomationElement.ControlTypeProperty,
                        ctlType
                    ),
                    new PropertyCondition(
                        AutomationElement.NameProperty,
                        name
                    )
                )
           );
        }

        /// <summary>
        /// Finds all the children with a given control type.
        /// </summary>
        /// <param name="ctlType">The ControlType you wish to find</param>
        /// <returns></returns>
        public AutomationElementCollection FindAllByControlType(ControlType ctlType) {
            return Element.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(
                    AutomationElement.ControlTypeProperty,
                    ctlType
                )
            );
        }

        #region Pattern Helpers

        private static void CheckNullElement(AutomationElement element) {
            if (element == null) {
                Console.WriteLine("Attempting to invoke pattern on null element");
                AutomationWrapper.DumpVS();
                throw new InvalidOperationException();
            }
        }

        /// <summary>
        /// Invokes the specified invokable item.  The item must support the invoke pattern.
        /// </summary>
        public static void Invoke(AutomationElement button) {
            CheckNullElement(button);
            var invokePattern = (InvokePattern)button.GetCurrentPattern(InvokePattern.Pattern);
            invokePattern.Invoke();
        }

        /// <summary>
        /// Selects the selectable item.  The item must support the Selection item pattern.
        /// </summary>
        /// <param name="selectionItem"></param>
        public static void Select(AutomationElement selectionItem) {
            CheckNullElement(selectionItem);
            var selectPattern = (SelectionItemPattern)selectionItem.GetCurrentPattern(SelectionItemPattern.Pattern);
            selectPattern.Select();
        }

        /// <summary>
        /// Selects the selectable item.  The item must support the Selection item pattern.
        /// </summary>
        /// <param name="selectionItem"></param>
        public static void AddToSelection(AutomationElement selectionItem) {
            CheckNullElement(selectionItem);
            var selectPattern = (SelectionItemPattern)selectionItem.GetCurrentPattern(SelectionItemPattern.Pattern);
            selectPattern.AddToSelection();
        }

        /// <summary>
        /// Selects the selectable item.  The item must support the Selection item pattern.
        /// </summary>
        public void Select() {
            Select(Element);
        }

        /// <summary>
        /// Expands the selected item.  The item must support the expand/collapse pattern.
        /// </summary>
        /// <param name="node"></param>
        public static void EnsureExpanded(AutomationElement node) {
            CheckNullElement(node);
            ExpandCollapsePattern pat = (ExpandCollapsePattern)node.GetCurrentPattern(ExpandCollapsePattern.Pattern);
            if (pat.Current.ExpandCollapseState == ExpandCollapseState.Collapsed) {
                pat.Expand();
            }
        }

        /// <summary>
        /// Collapses the selected item.  The item must support the expand/collapse pattern.
        /// </summary>
        /// <param name="node"></param>
        public static void Collapse(AutomationElement node) {
            CheckNullElement(node);
            ExpandCollapsePattern pat = (ExpandCollapsePattern)node.GetCurrentPattern(ExpandCollapsePattern.Pattern);
            if (pat.Current.ExpandCollapseState != ExpandCollapseState.Collapsed) {
                pat.Collapse();
            }
        }

        /// <summary>
        /// Gets the specified value from this element.  The element must support the value pattern.
        /// </summary>
        /// <returns></returns>
        public string GetValue() {
            return ((ValuePattern)Element.GetCurrentPattern(ValuePattern.Pattern)).Current.Value;
        }

        /// <summary>
        /// Sets the specified value from this element.  The element must support the value pattern.
        /// </summary>
        /// <returns></returns>
        public void SetValue(string value) {
            ((ValuePattern)Element.GetCurrentPattern(ValuePattern.Pattern)).SetValue(value);
        }

        #endregion

        /// <summary>
        /// Dumps the current top-level window in VS
        /// </summary>
        public static void DumpVS() {
            IVsUIShell uiShell = VsIdeTestHostContext.ServiceProvider.GetService(typeof(IVsUIShell)) as IVsUIShell;
            IntPtr hwnd;
            uiShell.GetDialogOwnerHwnd(out hwnd);
            AutomationWrapper.DumpElement(AutomationElement.FromHandle(hwnd));

            // if we have a dialog open dump the main VS window too
            var mainHwnd = new IntPtr(VsIdeTestHostContext.Dte.MainWindow.HWnd);
            if (mainHwnd != hwnd) {
                Console.WriteLine("VS: ");
                AutomationWrapper.DumpElement(AutomationElement.FromHandle(mainHwnd));
            }
        }

        public static void DumpElement(AutomationElement element) {
            Console.WriteLine("Name    ClassName      ControlType    AutomationID");
            DumpElement(element, 0);
        }

        private static void DumpElement(AutomationElement element, int depth) {
            Console.WriteLine(String.Format(
                "{0} {1}\t{2}\t{3}\t{4}", 
                new string(' ', depth * 4), 
                element.Current.Name, 
                element.Current.ControlType.ProgrammaticName, 
                element.Current.ClassName,
                element.Current.AutomationId
            ));

            var children = element.FindAll(TreeScope.Children, Condition.TrueCondition);
            foreach (AutomationElement child in children) {
                DumpElement(child, depth + 1);
            }
        }

        public void SetFocus() {
            Element.SetFocus();
        }

        public void Invoke() {
            Invoke(Element);
        }
    }

    public static class AutomationElementExtensions {
        public static AutomationWrapper AsWrapper(this AutomationElement element) {
            if (element == null) {
                return null;
            }
            return new AutomationWrapper(element);
        }

        public static void Select(this AutomationElement element) {
            AutomationWrapper.Select(element);
        }

        public static void EnsureExpanded(this AutomationElement node) {
            AutomationWrapper.EnsureExpanded(node);
        }

        public static void Collapse(this AutomationElement node) {
            AutomationWrapper.Collapse(node);
        }
    }
} 
