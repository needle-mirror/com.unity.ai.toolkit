using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.AI.Toolkit.Accounts.Services;
using Unity.AI.Toolkit.Accounts.Services.Core;

namespace Unity.AI.Toolkit.Accounts.Components
{
    [UxmlElement]
    partial class Points : VisualElement
    {
        readonly Label m_Points;
        Action m_Unsubscribe;

        public Points()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.ai.toolkit/Modules/Accounts/Components/Points/Points.uxml");
            tree.CloneTree(this);

            m_Points = this.Q<Label>(className: "points-label");
            this.Q<Button>("get-points").clicked += AccountLinks.GetPoints;

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                m_Unsubscribe = Account.pointsBalance.settings.Use(_ => RefreshPoints());
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                m_Unsubscribe?.Invoke();
                m_Unsubscribe = null;
            });
        }

        void RefreshPoints()
        {
            if (Account.pointsBalance.Value != null)
                m_Points.text = PrettyFormatSimple(Account.pointsBalance.Value.PointsAvailable);
        }

        static string PrettyFormatSimple(long number) => number.ToString("N0", CultureInfo.CurrentCulture.NumberFormat);
    }
}