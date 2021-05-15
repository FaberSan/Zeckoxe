﻿// Copyright (c) 2019-2020 Faber Leonardo. All Rights Reserved. https://github.com/FaberSanZ
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)


/*=============================================================================
	Mesh.cs
=============================================================================*/

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Vultaik.GLTF.Schema
{
    public class Mesh
    {
        [JsonPropertyName("primitives")]
        public MeshPrimitive[] Primitives { get; set; }



        [JsonPropertyName("weights")]
        public float[] Weights { get; set; }



        [JsonPropertyName("name")]
        public string Name { get; set; }



        [JsonPropertyName("extensions")]
        public Dictionary<string, object> Extensions { get; set; }



        [JsonPropertyName("extras")]
        public Extras Extras { get; set; }

      
    }
}
