using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

namespace UnityEngine
{
	public static class WhatUsesThis
	{
		const string CacheFilename = "Temp/WhatUsesThis.bin";

		static Dictionary<string, List<string>> _dict;
		static Dictionary<string, List<string>> Dict => _dict ?? Load() ?? CleanBuild();

		[MenuItem( "Window/What Uses This/Rebuild" )]
		static Dictionary<string, List<string>> CleanBuild()
		{
			try
			{
				var sw = System.Diagnostics.Stopwatch.StartNew();

				EditorUtility.DisplayProgressBar( "WhatUsesThis", "Getting Assets", 0.2f );

				var allAssets = AssetDatabase.FindAssets( "" ).Select( x => AssetDatabase.GUIDToAssetPath( x ) ).Distinct().ToArray();

				var dependancies = new Dictionary<string, string[]>();

				var i = 0;
				foreach ( var asset in allAssets )
				{
					dependancies[asset] = AssetDatabase.GetDependencies( asset, false );
					i++;

					if ( i%100 == 0 && EditorUtility.DisplayCancelableProgressBar( "WhatUsesThis", $"Getting Dependancies [{i}/{allAssets.Length}]", i / (float)allAssets.Length ) )
						return new Dictionary<string, List<string>>();
				}

				EditorUtility.DisplayProgressBar( "WhatUsesThis", "Building Dependants", 0.9f );

				_dict = new Dictionary<string, List<string>>();
				foreach ( var d in dependancies )
				{
					foreach ( var dependant in d.Value )
					{
						if ( !_dict.TryGetValue( dependant, out var list ) )
						{
							list = new List<string>();
							_dict[dependant] = list;
						}

						list.Add( d.Key );
					}
				}

				//Debug.Log( $"Generating took {sw.Elapsed.TotalSeconds}s" );

				Save();

				return _dict;
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		static void Save()
		{
			if ( _dict == null )
				return;

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using ( var stream = new System.IO.FileStream( CacheFilename, System.IO.FileMode.Create ) )
			{
				BinaryFormatter bin = new BinaryFormatter();
				bin.Serialize( stream, _dict );
			}

			//Debug.Log( $"Saving took {sw.Elapsed.TotalSeconds}s" );
		}

		static Dictionary<string, List<string>> Load()
		{
			try
			{
				var sw = System.Diagnostics.Stopwatch.StartNew();
				using ( var stream = new System.IO.FileStream( CacheFilename, System.IO.FileMode.Open ) )
				{
					BinaryFormatter bin = new BinaryFormatter();
					_dict = (Dictionary<string, List<string>>)bin.Deserialize( stream );
				}
				//Debug.Log( $"Loading took {sw.Elapsed.TotalSeconds}s" );
			}
			catch ( System.Exception )
			{
				_dict = null;
			}

			return _dict;
		}

		[MenuItem( "Assets/What uses this?" )]
		private static void FindParentAssets()
		{
			int iCount = 0;

			foreach ( var selectedObj in Selection.GetFiltered( typeof( UnityEngine.Object ), SelectionMode.Assets ) )
			{
				var selected = AssetDatabase.GetAssetPath( selectedObj );

				Debug.Log( $"<color=#5C93B9>What uses <b>{selected}</b>?</color>", selectedObj );

				if ( Dict.TryGetValue( selected, out var dependants ) )
				{
					foreach ( var d in dependants )
					{
						Debug.Log( $"<color=#8CA166>  {d}</color>", AssetDatabase.LoadAssetAtPath<Object>( d ) );
						iCount++;
					}
				}
			}

			Debug.Log( $"<color=#5C93B9>Search complete, found <b>{iCount}</b> result{( iCount == 1 ? "" : "s" )}</color>");
		}

		[MenuItem( "Assets/What does this use?" )]
		private static void FincChildAssets()
		{
			int iCount = 0;

			foreach ( var selectedObj in Selection.GetFiltered( typeof( UnityEngine.Object ), SelectionMode.Assets ) )
			{
				var selected = AssetDatabase.GetAssetPath( selectedObj );

				Debug.Log( $"<color=#5C93B9>What does <b>{selected}</b> use?</color>", selectedObj );

				foreach ( var d in AssetDatabase.GetDependencies( selected, false ) )
				{
					Debug.Log( $"<color=#8CA166>  {d}</color>", AssetDatabase.LoadAssetAtPath<Object>( d ) );
					iCount++;
				}
				
			}

			Debug.Log( $"<color=#5C93B9>Search complete, found <b>{iCount}</b> result{( iCount == 1 ? "" : "s" )}</color>" );
		}
	}
}
