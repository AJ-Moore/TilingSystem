using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TileSystem { 
    public class AtlasTexture : ScriptableObject {

        public Material material;
        public Vector2 uvSubDivisions = Vector2.one;
    }

    // provides interface for looking up tile material by ID
    public class AtlasTextureMap {
        /// <summary>
        /// Used for textel correction, avoid bleeding.
        /// </summary>
        public uint textureSize = 32;

        Dictionary<uint, TileTexture> voxelDataMap = new Dictionary<uint, TileTexture>();

        private uint _textureIDCount = 0;

        public uint TextureIDCount {
            get { return _textureIDCount; }
        }

        // returns the texture for the given ID in the map. 
        public TileTexture getTextureForID(uint ID) {
            return voxelDataMap[ID];
        }

        // Add texture to the mapping
        public void addTexture(AtlasTexture texture) {

            float xDiv = (texture.uvSubDivisions.x > 0) ? texture.uvSubDivisions.x : 1;
            float yDiv = (texture.uvSubDivisions.y > 0) ? texture.uvSubDivisions.y : 1;

            float subSizeX = 1.0f / xDiv;
            float subSizeY = 1.0f / yDiv;

            float textelOffset = subSizeX / 64.0f;

            // create a map entry for every atlas image, increment count
            for (uint i = 0; i < xDiv; ++i) {
                for (uint p = 0; p < yDiv; ++p) {
                    TileTexture vTexture = new TileTexture();
                    vTexture.material = texture.material;

                    // Bottom Left
                    vTexture.textureCoords[0].x = (i * subSizeX) + textelOffset;
                    vTexture.textureCoords[0].y = (p * subSizeY) + textelOffset;

                    // Top Left
                    vTexture.textureCoords[1].x = (i * subSizeX) + textelOffset;
                    vTexture.textureCoords[1].y = ((p * subSizeY) + subSizeY) - textelOffset;

                    // Top Right
                    vTexture.textureCoords[2].x = ((i * subSizeX) + subSizeX) - textelOffset;
                    vTexture.textureCoords[2].y = ((p * subSizeY) + subSizeY) - textelOffset;

                    // Bottom Right
                    vTexture.textureCoords[3].x = ((i * subSizeX) + subSizeX) - textelOffset;
                    vTexture.textureCoords[3].y = (p * subSizeY) + textelOffset;

                    this.voxelDataMap.Add(_textureIDCount, vTexture);

                    _textureIDCount++;
                }
            }

        }

    }
}
