using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TileSystem {

    /// <summary>
    /// These define the seperate layers in the map. In asending order of render order!
    /// </summary>
    public enum LayerType {
        layer_floor,
        layer_decal,
        layer_foreground,
    }

    /// <summary>
    /// Tile layers make up a tile map, a TileLayer is an aggregate of TileChunk /s
    /// </summary>
    public class TileLayer {

        /// Gameobject representation in the editor 
        public GameObject gameObject;

        /// The parent map to which this layer belongs 
        TileMap parent;

        /// The chunks that make up this tile layer.
        List<TileChunk> chunks = new List<TileChunk>();

        uint layerDimensions = 128;

        uint subMeshSize = 0;

        uint chunkSize = 32;

        bool isDirty = false;

        uint layerType = 0;
        public uint LayerType {
            get { return layerType;  }
            set { layerType = value; }
        }

        public void setParent(TileMap Parent) {
            this.parent = Parent;
        }

        public TileMap getParent () {
            return this.parent; 
        }

        public List<TileChunk> getChunks() {
            return this.chunks;
        }

        // Set the layer dirty, trigger update of layer
        public void setDirty(bool isDirty) {
            this.isDirty = isDirty;
        }

        public void setTile(Vector2 Position, Tile Tile) {
            // Work out which chunk
            uint chunkX = (uint)(Position.x / (float)this.chunkSize);
            uint chunkY = (uint)(Position.y / (float)this.chunkSize);

            // Optimise precalculate this

            uint _chunkDimension = this.layerDimensions / this.chunkSize;

            uint pos = (chunkX) + ((chunkY * _chunkDimension) );

            // Normalise the position to make relative to parent chunk.
            Vector2 normalisedPosition = Position - new Vector2(chunkX * this.chunkSize, chunkY * this.chunkSize);

            this.chunks[(int)pos].setTile(normalisedPosition, Tile);
        }

        public TileLayer(uint layerDimensions, uint chunkSize, uint subMeshSize) {
            this.layerDimensions = layerDimensions;
            this.chunkSize = chunkSize; 
        }

        public void init() {
            this.gameObject = new GameObject("Layer: Add layer name here!");
            this.gameObject.transform.parent = this.parent.transform;

            // Create the chunks


            for (uint i = 0; i < (this.layerDimensions / this.chunkSize); ++i) {
                for (uint p = 0; p < (this.layerDimensions / this.chunkSize); ++p) {
                   

                    TileChunk chunk = this.addChunk(new TileChunk(this.chunkSize, (uint)subMeshSize));
                    chunk.position = new Vector3((p * this.chunkSize), 
                                                 (i * this.chunkSize), 
                                                 (0));

                    chunk.init();
                    chunk.updateBuffer();
                    
                }
            }

        }

        public TileChunk addChunk(TileChunk chunk) {
            chunk.setParent(this);
            this.chunks.Add(chunk);
            return chunk;
        }

        public void updateChunks() {
            foreach (TileChunk chunk in this.chunks) {
                chunk.update();
                chunk.performTileUpdates();
            }
        }

    }
}