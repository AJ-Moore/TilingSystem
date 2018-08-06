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

        // Are we all ready fetching a tile from this chunk? Yeh then id's invalid ( failsafe/ paranoia)
        bool fetchTileInProgress = false; 

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

            float chunkOrigin = this.chunkSize * 0.5f;
            float unitScale = this.Parent.getParent().tileUnitSize;

            this.mesh = new Mesh();
            this.geometry = new GameObject("TileChunk");
            this.geometry.transform.parent = this.parent.gameObject.transform;
            this.geometry.transform.position = this.position + new Vector3(chunkOrigin * unitScale, chunkOrigin* unitScale, 0);
            this.geometry.transform.localScale = Vector3.one * unitScale;

            this.meshRenderer = this.geometry.AddComponent<MeshRenderer>();
            this.meshFilter = this.geometry.AddComponent<MeshFilter>();

            this.meshRenderer.material = this.parent.getParent().textureMap.getTextureForID(0).material;

            int chunkOffset = -(int)this.chunkSize / 2;

            // Add row by row
            for (int y = 0; y < this.chunkSize; ++y) {
                for (int x = 0; x < this.chunkSize; ++x) {

                    // Create a new tile
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

            // Call init on all tiles in  the map!
            foreach(TileDat Tile in tiles) {
                foreach(TileBehaviour behaviour in Tile.tile.behaviours) {
                    behaviour.onInit(Tile);
                }
            }
           
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

            // Experimental [:
            generateColliders(); 
        }

        //Block update variables, number of blocks that update per update. 
        static int blockUpdatesPerCycle = 20;
        int currentBlock = 0;
        float tileUpdateTime = 0;

        // returns true if this chunk has finished updating. >> called by tile map.
        public bool performTileUpdates() {

            int start = currentBlock;

            // this can be optimised to update a list of tiles to update < todo 
            for (int i = currentBlock; i < this.tiles.Count; ++i) {// && i < start + blockUpdatesPerCycle; ++i) {

                currentBlock++;

                if (this.tiles[i].tile.prop != TilePropType.prop_simulated) { 
                    continue;
                }

                foreach (TileBehaviour behaviour in this.tiles[i].tile.behaviours) {
                    behaviour.onUpdate(this.tiles[i]);
                    behaviour.onUpdate(this.tiles[i], tileUpdateTime);
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

        // ensure consistency when getting the array position for the tile.
        public int vectorToArrayPosition(Vector2 Position) {
            return (int)(Position.x + ((float)this.chunkSize * Position.y));
        }

        // because it's faster
        public int vectorToArrayPosition(float x, float y) {
            return (int)(x + ((float)this.chunkSize * y));
        }

        // Overload 2d [][] operator!
        public TileDat this[int x, int y] {
            get { return tiles[vectorToArrayPosition(x,y)]; }
            set { tiles[vectorToArrayPosition(x, y)] = value; }
        }


        public TileDat getTileAtPosition(Vector2 Position, bool makeDirty = false) {
            // Get the position in the array of tiles.
            int pos = this.vectorToArrayPosition(Position);

            bool outsideRange = false;

            // end of the chunk
            if (Position.x < 0 || Position.y < 0 || Position.x >= this.chunkSize || Position.y >= this.chunkSize) {
                outsideRange = true;
            }

            if (pos >= this.tiles.Capacity || pos < 0 || outsideRange) {

                // Anti recursion check, catches invalid 'states'
                if (this.fetchTileInProgress) {
                    // bail! < TODO return default dat 
                    TileDat transparent = new TileDat();
                    transparent.tile = new Tile();
                    transparent.tile.prop = TilePropType.prop_air;
                    this.fetchTileInProgress = false;
                    return transparent;
                }

                // try and fetch from parent! slow af, mark fetch to handle fail state 
                this.fetchTileInProgress = true; 
                return this.parent.getParent().getTileAtPosition((Vector2)this.position + Position, (LayerType)this.parent.LayerType, makeDirty);
                this.fetchTileInProgress = false;

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
                // Anti recursion check, catches invalid 'states'
                if (this.fetchTileInProgress) {
                    // bail! < do nothing
                    return;
                }

                this.fetchTileInProgress = true;
                this.parent.getParent().setTile((Vector2)this.position + Position, Tile);
                this.fetchTileInProgress = false;
                return;
            }

            // Call on destroy for behaviours
            foreach (TileBehaviour behaviour in this.tiles[pos].tile.behaviours) {
                behaviour.onDestroy(this.tiles[pos]);
            }

            // Get the tile data object.
            TileDat tileData = this.tiles[pos];
            tileData.flags = 0;
            tileData.tile = Tile;

            updateAjacentTiles(Position);

            // Call on init for behaviours
            foreach (TileBehaviour behaviour in Tile.behaviours) {
                behaviour.onInit(tileData);
            }

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
            TileDat current = this.getTileAtPosition( Position);
            TileDat up = this.getTileAtPosition(Position + Vector2.up);
            TileDat down = this.getTileAtPosition(Position - Vector2.up);
            TileDat right = this.getTileAtPosition(Position + Vector2.right);
            TileDat left = this.getTileAtPosition(Position - Vector2.right);

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

        // Experimental method for generating colliders using a <something> collider! [:
        void generateColliders() {

            Vector3[] vertices = this.mesh.vertices;

            // Faster than casting iterate through 
            for (uint i = 0; i < vertices.Length; i += 6) {

                // Block out a tile
                EdgeCollider2D edgeCollider = this.geometry.AddComponent<EdgeCollider2D>();
                Vector2[] edgePoints = new Vector2[8];
                // Left edge 
                edgePoints[0].x = vertices[i].x;
                edgePoints[0].y = vertices[i].y;
                edgePoints[1].x = vertices[i + 1].x;
                edgePoints[1].y = vertices[i + 1].y;

                // Top edge
                edgePoints[2].x = vertices[i + 1].x;
                edgePoints[2].y = vertices[i + 1].y;
                edgePoints[3].x = vertices[i + 2].x;
                edgePoints[3].y = vertices[i + 2].y;

                // Right edge 
                edgePoints[4].x = vertices[i + 2].x;
                edgePoints[4].y = vertices[i + 2].y;
                edgePoints[5].x = vertices[i + 5].x;
                edgePoints[5].y = vertices[i + 5].y;

                // Bottom edge
                edgePoints[6].x = vertices[i + 5].x;
                edgePoints[6].y = vertices[i + 5].y;
                edgePoints[7].x = vertices[i + 0].x;
                edgePoints[7].y = vertices[i + 0].y;

                edgeCollider.points = edgePoints;

            }



        }

        void renderPass(RenderFlags RenderPass) {

            // Calculate vertex buffer size. 6 per face
            uint vertexBufferSize = 6 * (this.chunkSize * this.chunkSize);

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();

            int vertexOffset = 0;
            int chunkOffset = -(int)this.chunkSize / 2;


            for (int x = 0; x < this.chunkSize; ++x) {
                for (int y = 0; y < this.chunkSize; ++y) {

                    int z = 0;

                    // Tile were rendering 
                    TileDat tile = this[x,y];

                    // DON'T render air -> this should be figured out earlier then pushed to be rendered or simplified
                    if (!tile.tile.rendered || tile.tile.prop == TilePropType.prop_air || tile.tile.prop == TilePropType.prop_vacumn) {
                        continue;
                    }

                    // Used to determine the vertex offset & whether we need to add to the sort list ( not used )
                    byte _offset = 0;

                    // Back 
                    vertices.Add(new Vector3(x + chunkOffset, y + chunkOffset, z ));
                    vertices.Add(new Vector3(x + chunkOffset, y + chunkOffset + 1, z));
                    vertices.Add(new Vector3(x + chunkOffset + 1, y + chunkOffset + 1, z ));
                    vertices.Add(new Vector3(x + chunkOffset, y + chunkOffset, z ));
                    vertices.Add(new Vector3(x + chunkOffset + 1, y + chunkOffset + 1, z ));
                    vertices.Add(new Vector3(x + chunkOffset + 1, y + chunkOffset, z ));

                    uvs.Add(tile.tile.material.textureCoords[0]);
                    uvs.Add(tile.tile.material.textureCoords[1]);
                    uvs.Add(tile.tile.material.textureCoords[2]);
                    uvs.Add(tile.tile.material.textureCoords[0]);
                    uvs.Add(tile.tile.material.textureCoords[2]);
                    uvs.Add(tile.tile.material.textureCoords[3]);

                    normals.Add(Vector3.forward);
                    normals.Add(Vector3.forward);
                    normals.Add(Vector3.forward);
                    normals.Add(Vector3.forward);
                    normals.Add(Vector3.forward);
                    normals.Add(Vector3.forward);

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
            //this.mesh.triangles = triangles;
            this.mesh.SetTriangles(triangles, 0, false);
            this.mesh.normals = normals.ToArray();

        }

    }
}