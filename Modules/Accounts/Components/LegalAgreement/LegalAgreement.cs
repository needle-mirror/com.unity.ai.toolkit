using System;
using System.Collections.Generic;
using Unity.AI.Toolkit.Accounts.Services;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Components
{
    [UxmlElement]
    partial class LegalAgreement : VisualElement
    {
        record AIData
        {
            public string text;
            public List<LabelUrlLink> links;
            public List<string> packages;
            public string installButtonText;
            public string noInternet;
            public string installingPackages;
        }

        AIData m_Data = new()
        {
            text = "I have read and agree to the <link=terms><color=#7BAEFA>Unity AI Terms of Service</color></link> and the <link=supplemental><color=#7BAEFA>Generative AI Supplemental Privacy Notice.</color></link>"
                + "\n\nI acknowledge and understand Unity AI uses <link=thirdparty><color=#7BAEFA>these third-party services</color></link>.",
            links = new()
            {
                new() {id = "terms", url = "https://unity.com/legal/terms-of-service"},
                new() {id = "supplemental", url = "https://unity.com/legal/supplemental-privacy-statement-unity-muse"},
                new() {id = "thirdparty", url = "https://unity.com/legal/terms-of-service"}
            },
            noInternet = "You need an internet connection to be able to use the AI features.",
            installingPackages = "Installing packages",

            packages = new()
            {
                "com.unity.ai.generators",
                "com.unity.ai.assistant"
            },

            installButtonText = "Agree and install AI features"
        };

        public LegalAgreement()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.ai.toolkit/Modules/Accounts/Components/LegalAgreement/LegalAgreement.uxml");
            tree.CloneTree(this);

            var text = this.Q<RichLabel>("legal-text");
            text.links = m_Data.links;
            text.text = m_Data.text;

            var button = this.Q<Button>("agree-button");
            button.text = m_Data.installButtonText;
            button.clicked += () => _ = AccountController.SetTermsOfService();

            Add(text);
            Add(button);
        }
    }
}
