using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tomatech.AFASS.Editor
{
    [CustomPropertyDrawer(typeof(AddressableKeyAttribute))]
    public class AddressableKeyAttributePropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            //return base.CreatePropertyGUI(property);
            var prop = new TextField
            {
                label = property.displayName
            };
            prop.BindProperty(property);
            prop.AddToClassList(BaseField<string>.alignedFieldUssClassName);

            void RefreshAddress()
            {
                if (property.stringValue != null)
                {
                    //property is indeed a string
                    prop.SetEnabled(false);
                    Object assetObject = null;
                    switch (property.serializedObject.targetObject)
                    {
                        case MonoBehaviour monoTargetObject:
                            if (AssetDatabase.GetAssetPath(monoTargetObject.gameObject) != null)
                                assetObject = monoTargetObject.gameObject;
                            break;
                        case ScriptableObject scriptableTargetObject:
                            assetObject = scriptableTargetObject;
                            break;
                    }
                    if (assetObject)
                    {
                        var assetGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(assetObject));
                        if (!AddressableAssetSettingsDefaultObject.Settings)
                        {
                            Debug.LogWarning("Addressables has not been set up. Please set it up using \"Window/Asset Management/Addressables/Groups\"");
                            return;
                        }
                        var addressableEntry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(assetGUID);
                        if (addressableEntry != null && property.stringValue != addressableEntry.address)
                        {
                            property.stringValue = addressableEntry.address;
                            property.serializedObject.ApplyModifiedProperties();
                            //Debug.Log(addressableEntry.address);
                        }
                    }
                }
            }

            RefreshAddress();

            void AddressableModificationEvent(AddressableAssetSettings a, AddressableAssetSettings.ModificationEvent m, object d)
            {
                try
                {
                    if (m == AddressableAssetSettings.ModificationEvent.EntryAdded ||
                        m == AddressableAssetSettings.ModificationEvent.EntryCreated ||
                        m == AddressableAssetSettings.ModificationEvent.EntryModified ||
                        m == AddressableAssetSettings.ModificationEvent.EntryMoved ||
                        m == AddressableAssetSettings.ModificationEvent.EntryRemoved)
                        RefreshAddress();
                }
                catch (System.NullReferenceException _)
                {
                    var test = _;
                    AddressableAssetSettings.OnModificationGlobal -= AddressableModificationEvent;
                }
            }

            AddressableAssetSettings.OnModificationGlobal += AddressableModificationEvent;



            return prop;
        }
    }
}