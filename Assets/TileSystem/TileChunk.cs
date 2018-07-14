using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TileSystem {

    public enum RenderFlags {
        DEFAULT
    }

    public class TileChunk {

        public Vector3 position;

        private uint chunkSize = 16;

        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;

        TileLayer parent;
        public TileLayer Parent {
            get { return parent; }
        }

        // Buffer is updated when dirty
        bool isDirty = false;

        // All the data for tiles within this chunk
        private List<TileDat> tiles = new List<TileDat>();

        // ref parent gameobject where mesh resides
        private GameObject geometry;

        public void setParent(TileLayer Parent) {
            this.parent = Parent;
        }

        public TileChunk(uint ChunkSize, uint subMeshSize) {
            this.chunkSize = ChunkSize;
        }

        public void init() {

            this.mesh = new Mesh();
            this.geometry = new GameObject("TileChunk");
            this.geometry.transform.parent = this.parent.gameObject.transform;
            this.geometry.transform.position = this.position + (Vector3.one * this.chunkSize * 0.5f);

            this.meshRenderer = this.geometry.AddComponent<MeshRenderer>();
            this.meshFilter = this.geometry.AddComponent<MeshFilter>();

            this.meshRenderer.material = this.parent.getParent().textureMap.getTextureForID(0).material;

            int chunkOffset = -(int)this.chunkSize / 2;

            // Add row by row
            for (int y = 0; y < this.chunkSize; ++y) {
                for (int x = 0; x < this.chunkSize; ++x) {

                    // Create a new voxel
                    TileDat tileToAdd = new TileDat();

                    // Parents layer parent is the tile map itself, default use first tile in map, which is air! 
                    tileToAdd.tile = this.parent.getParent().getTile(0);
                    tileToAdd.parent = this;

                    // tiles are offset by chunk offset, so the gameobjects center is in the center of the chunk
                    tileToAdd.position = new Vector3(x + chunkOffset, y + chunkOffset, 0) + this.geometry.transform.position;
                    tiles.Add(tileToAdd);

                }
            }

            // updates enture tile layer!
            updateAjacentTiles();
        }

        public void update() {

            if (this.isDirty) {

                this.updateBuffer();
                this.isDirty = false;
            }

        }


        // Updates the vertex data of the mesh.
        public void updateBuffer() {
            this.mesh = this.geometry.GetComponent<MeshFilter>().mesh;
            this.mesh.Clear();
            renderPass(RenderFlags.DEFAULT);
        }

        //Block update variables, number of blocks that update per update. 
        static int blockUpdatesPerCycle = 200;
        int currentBlock = 0;
        float tileUpdateTime = 0;

        // returns true if this chunk has finished updating. >> called by voxel map.
        public bool performTileUpdates() {

            int start = currentBlock;

            // this can be optimised to update a list of tiles to update < todo 
            for (int i = currentBlock; i < this.tiles.Count && i < start + blockUpdatesPerCycle; ++i) {

                currentBlock++;

                if (this.tiles[i].tile.prop != TilePropType.prop_simulated) { 
                    continue;
                }

                foreach (TileBehaviour behaviour in this.tiles[i].tile.behaviours) {
                    behaviour.blockUpdate(this.tiles[i], tileUpdateTime);
                }
                
            }

            if (currentBlock >= this.tiles.Count) {
                currentBlock = 0;
                tileUpdateTime = Time.time;
                return true;
            } else {
                return false;
            }

        }

        // ensure consistency when getting the array position for the voxel.
        public int vectorToArrayPosition(Vector2 Position) {
            return (int)(Position.x + ((float)this.chunkSize * Position.y));
        }

        public TileDat getTileAtPosition(Vector2 Position) {
            // Get the position in the array of voxels.
            int pos = this.vectorToArrayPosition(Position);

            bool outsideRange = false;

            // end of the chunk
            if (Position.x < 0 || Position.y < 0 || Position.x >= this.chunkSize || Position.y >= this.chunkSize) {
                outsideRange = true;
            }

            if (pos >= this.tiles.Capacity || pos < 0 || outsideRange) {
                // return air block, this can/needs be optimised!! < Todo
                Debug.LogWarning("Could not find tile specified!");
                TileDat air = new TileDat();
                air.tile = new Tile();
                air.flags |= (uint)TilePropType.prop_air;
                return air;
            }

            return this.tiles[pos];
        }

        public void setTile(Vector2 Position, Tile Tile) {
            int pos = this.vectorToArrayPosition(Position);

            bool outsideRange = false;

            // end of the chunk
            if (Position.x < 0 || Position.y < 0 || Position.x >= this.chunkSize || Position.y >= this.chunkSize) {
                outsideRange = true;
            }

            if (pos >= this.tiles.Capacity || pos < 0 || outsideRange) {
                //Debug.LogError("Unable to set voxel at position specified");
                return;
            }

            // Get the voxel data object.
            TileDat tileData = this.tiles[pos];
            tileData.flags = 0;
            tileData.tile = Tile;

            updateAjacentTiles(Position);


            // set the buffer dirty.
            this.isDirty = true;
        }

        public void updateAjacentTiles() {
            for (int x = 0; x < this.chunkSize; ++x) {
                for (int y = 0; y < this.chunkSize; ++y) {
                    this.updateAjacentTiles(new Vector2(x, y));
                }
            }
        }

        void updateAjacentTiles(Vector2 Position) {

            // Optimise this don't do a parent fetch if we know it is in bounds todo < will make call alot faster most the time 
            TileDat current = this.parent.getParent().getTileAtPosition((Vector2)this.position + Position, (LayerType)this.parent.LayerType, false );
            TileDat up = this.parent.getParent().getTileAtPosition((Vector2)this.position + Position + Vector2.up, (LayerType)this.parent.LayerType, false);
            TileDat down = this.parent.getParent().getTileAtPosition((Vector2)this.position + Position - Vector2.up, (LayerType)this.parent.LayerType, false);
            TileDat right = this.parent.getParent().getTileAtPosition((Vector2)this.position + Position + Vector2.right, (LayerType)this.parent.LayerType, false);
            TileDat left = this.parent.getParent().getTileAtPosition((Vector2)this.position + Position - Vector2.right, (LayerType)this.parent.LayerType, false);

            current.left = left;
            current.right = right;
            current.up = up;
            current.down = down;

            down.up = current;
            up.down = current;
            left.right = current;
            right.left = current; 


        }

        public void setDirty(bool dirty = true) {
            this.isDirty = dirty; 
        }

        void renderPass(RenderFlags RenderPass) {

            // Calculate vertex buffer size. 6 per face
            uint vertexBufferSize = 6 * (this.chunkSize * this.chunkSize);

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();

            int vertexOffset = 0;
            int chunkOffset = -(int)this.chunkSize / 2;

            // This could be more optimal. 

            for (int x = 0; x < this.chunkSize; ++x) {
                for (int y = 0; y < this.chunkSize; ++y) {

                    int z = 0;

                    // Tile were rendering 
                    TileDat tile = this.getTileAtPosition(new Vector2(x, y));

                    // DON'T render air
                    if (!tile.tile.rendered || tile.tile.prop == TilePropType.prop_air || tile.tile.prop == TilePropType.prop_vacumn) {
                        continue;
                    }

                    // Used to determine the vertex offset & whether we need to add to the sort list ( not used )
                    byte _offset = 0;

                    // Back 
                    vertices.Add(new Vector3(x + chunkOffset + 1, y + chunkOffset, z + 0 + chunkOffset));
                    vertices.Add(new Vector3(x + chunkOffset + 1, y + chunkOffset + 1, z + 0 + chunkOffset));
                    vertices.Add(new Vector3(x + chunkOffset, y + chunkOffset + 1, z + 0 + chunkOffset));
                    vertices.Add(new Vector3(x + chunkOffset + 1, y + chunkOffset, z + 0 + chunkOffset));
                    vertices.Add(new Vector3(x + chunkOffset, y + chunkOffset + 1, z + 0 + chunkOffset));
                    vertices.Add(new Vector3(x + chunkOffset, y + chunkOffset, z + 0 + chunkOffset));

                    uvs.Add(tile.tile.material.textureCoords[0]);
                    uvs.Add(tile.tile.material.textureCoords[1]);
                    uvs.Add(tile.tile.material.textureCoords[2]);
                    uvs.Add(tile.tile.material.textureCoords[0]);
                    uvs.Add(tile.tile.material.textureCoords[2]);
                    uvs.Add(tile.tile.material.textureCoords[3]);

                    normals.Add(Vector3.back);
                    normals.Add(Vector3.back);
                    normals.Add(Vector3.back);
                    normals.Add(Vector3.back);
                    normals.Add(Vector3.back);
                    normals.Add(Vector3.back);

                    _offset += 6;

                    vertexOffset += _offset;


                }
            }

            int[] triangles = new int[vertexOffset];
            for (int i = 0; i < vertexOffset; ++i) {
                triangles[i] = i;
            }

            this.mesh.vertices = vertices.ToArray();
            this.mesh.uv = uvs.ToArray();
            this.mesh.triangles = triangles;
            this.mesh.normals = normals.ToArray();

        }

    }
}