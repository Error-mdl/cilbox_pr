using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections.Specialized;
using System.Collections;
using System.Runtime.InteropServices;
using System.Reflection;

namespace Cilbox
{
	[CilboxTarget]
	public class CilboxAvatar : Cilbox
	{
		public override long MaxTimeoutLengthUs => 5000; // 5ms. Avatas need to be restrictive.

		static HashSet<Type> whiteListType = new HashSet<Type>(){
			typeof(CilboxPublicUtils),
			typeof(System.Array),
			typeof(System.Boolean),
			typeof(System.Byte),
			typeof(System.Char),
			typeof(System.Collections.Generic.List<int>).GetGenericTypeDefinition(),
			typeof(System.Collections.Generic.Dictionary<int,int>).GetGenericTypeDefinition(),
			typeof(System.Collections.Generic.HashSet<int>).GetGenericTypeDefinition(),
			typeof(System.DateTime),
			typeof(System.DayOfWeek),
			typeof(System.Diagnostics.Stopwatch),
			typeof(System.Double),
			typeof(System.Int32),
			typeof(System.Int64),
			typeof(System.MathF),
			typeof(System.Math),
			typeof(System.Object),
			typeof(System.Single),
			typeof(System.String),
			typeof(System.TimeSpan),
			typeof(System.UInt16),
			typeof(System.UInt32),
			typeof(System.UInt64),
			typeof(System.ValueTuple),
			typeof(void),
			typeof(UnityEngine.Component),
			typeof(UnityEngine.Debug),
			typeof(UnityEngine.Events.UnityAction),
			typeof(UnityEngine.Events.UnityEvent),
			typeof(UnityEngine.GameObject),     // Hyper restrictive.
			typeof(UnityEngine.Material),
			typeof(UnityEngine.MaterialPropertyBlock),
			typeof(UnityEngine.Mathf),
			typeof(UnityEngine.MeshRenderer),
			typeof(UnityEngine.MonoBehaviour),   // Note this is needed for the 'ctor, but we can be very restrictive.
			typeof(UnityEngine.Object),
			typeof(UnityEngine.Random),
			typeof(UnityEngine.Renderer),
			typeof(UnityEngine.Time),
			typeof(UnityEngine.Texture),
			typeof(UnityEngine.UI.Button.ButtonClickedEvent),
			typeof(UnityEngine.UI.Button),
			typeof(UnityEngine.UI.InputField),
			typeof(UnityEngine.UI.InputField.OnChangeEvent),
			typeof(UnityEngine.UI.Scrollbar),
			typeof(UnityEngine.UI.Selectable),
			typeof(UnityEngine.UI.Slider),
			typeof(UnityEngine.UI.Text),
			typeof(UnityEngine.TextAsset),
			typeof(UnityEngine.Texture2D),
			typeof(UnityEngine.Transform),
			typeof(UnityEngine.Vector4),
			typeof(UnityEngine.Vector3),
		};

		readonly struct AllowedField : IEquatable<AllowedField>
		{
			public readonly Type type;
			public readonly string fieldName;

			public AllowedField(Type type, string fieldName)
			{
				this.type = type;
				this.fieldName = fieldName;
			}

			public readonly bool Equals(AllowedField other)
            {
                return (this.type == other.type) && (string.Equals(this.fieldName, other.fieldName, StringComparison.Ordinal));
            }

			public readonly override int GetHashCode()
			{
				return HashCode.Combine(type.GetHashCode(), fieldName.GetHashCode());
			}
        }

		static HashSet<AllowedField> whiteListFields = new HashSet<AllowedField>(){
			new( typeof(Vector3), "x"),
			new( typeof(Vector3), "y"),
			new( typeof(Vector3), "z"),
			new( typeof(Vector4), "x"),
			new( typeof(Vector4), "y"),
			new( typeof(Vector4), "z"),
			new( typeof(Vector4), "w"),
		};

		static public HashSet<Type> GetWhiteListTypes() { return whiteListType; }

		// This is called by CilboxUsage to decide of a type is allowed.
		// If a type is allowed, by defalt it is all allowed.
		override public bool CheckTypeAllowed( Type sType )
		{
			if (sType.IsPrimitive || sType.IsEnum) return true;

			if (sType.IsGenericType) sType = sType.GetGenericTypeDefinition();

			return whiteListType.Contains( sType );
		}

		override public bool CheckFieldAllowed( Type sType, String sFieldName )
		{
			if( !CheckTypeAllowed( sType ) ) return false;
			if( sFieldName.Length < 1 ) return false;
			if (sType.IsGenericType) sType = sType.GetGenericTypeDefinition();
			
			AllowedField field = new (sType, sFieldName);

			return whiteListFields.Contains(field);
		}

		// After a type is allowed, this is called to see if the specific method is OK.
		override public bool CheckMethodAllowed( out MethodInfo mi, Type declaringType, String name, SerializedTypeDescriptor [] parametersIn, SerializedTypeDescriptor [] genericArgumentsIn, String fullSignature )
		{
			mi = null;

			// You're allowed to get access to the constructor, nothing else.
			// We could selectively open up more methods on MonoBehaviour.
			if( declaringType == typeof(UnityEngine.MonoBehaviour) && name != ".ctor" ) return false;

			if( declaringType == typeof(UnityEngine.Events.UnityAction) && name != ".ctor" ) return false;

			if( declaringType == typeof(UnityEngine.GameObject) && name != "SetActive" ) return false;

			// UnityEngine.Object.Instantiate spawns a prefab tree verbatim, bypassing
			// host sanitization. A cilbox script can reference an unsanitized prefab
			// from a serialized field and its UnityEvents (e.g. Button.onClick ->
			// Application.OpenURL) execute outside the sandbox. Block all variants.
			if( declaringType == typeof(UnityEngine.Object) &&
				( name == "Instantiate" || name == "InstantiateAsync" ) )
				return false;

			if( name.Contains( "Invoke" ) ) return false;
			return true;
		}

        public override bool GetTypeOverride(string sType, out Type t)
        {
			t = null;
            return false;
        }
	}
}
