import os
import shutil

map_dir = "Assets/Resources/MapData"
if os.path.exists(map_dir):
    shutil.rmtree(map_dir)
os.makedirs(map_dir)

def generate_csv(filename, blocks):
    filepath = os.path.join(map_dir, filename)
    with open(filepath, "w") as f:
        f.write("x,z,y,type,extraOption,cost,tags\n")
        for b in blocks:
            f.write(f"{b['x']},{b['z']},{b['y']},{b['type']},{b.get('ext','')},{b.get('cost','')},{b.get('tags','')}\n")

def make_map(stage_num, start, end, floors, obstacles, tags, max_cost):
    blocks = []
    # Start
    blocks.append({'x': start[0], 'z': start[1], 'y': 0, 'type': 'Start', 'ext': start[2], 'cost': max_cost, 'tags': tags})
    # Floors & Obstacles
    placed_floors = set()
    
    for o in obstacles:
        blocks.append({'x': o[0], 'z': o[1], 'y': 0, 'type': o[2]})
        if (o[0], o[1]) not in placed_floors:
            blocks.append({'x': o[0], 'z': o[1], 'y': 0, 'type': 'Floor'})
            placed_floors.add((o[0], o[1]))
            
    for f in floors:
        if (f[0], f[1]) not in placed_floors:
            blocks.append({'x': f[0], 'z': f[1], 'y': 0, 'type': 'Floor'})
            placed_floors.add((f[0], f[1]))
            
    # End
    blocks.append({'x': end[0], 'z': end[1], 'y': 0, 'type': 'End'})
    
    generate_csv(f"Stage{stage_num:02d}_Map.csv", blocks)

# Stage 01: Simple Forward
make_map(1, (0,0,90), (4,0), [(1,0), (2,0), (3,0)], [], "Sequence|Spatial", 15)
make_map(2, (0,0,90), (3,-3), [(1,0), (2,0), (3,0), (3,-1), (3,-2)], [], "Sequence|Spatial", 15)
make_map(3, (0,0,90), (3,3), [(1,0), (1,1), (2,1), (2,2), (3,2)], [], "Sequence|Spatial", 15)
make_map(4, (0,0,90), (2,-5), [(1,0), (2,0), (2,-1), (1,-1), (1,-2), (2,-2), (2,-3), (1,-3), (1,-4), (2,-4)], [], "Sequence|Spatial", 25)
make_map(5, (0,0,90), (3,3), [(1,0), (1,1), (2,1), (2,2), (3,2)], [], "Function", 15)

floors6 = [(i,0) for i in range(1, 10)]
make_map(6, (0,0,90), (10,0), floors6, [], "While", 15)

make_map(7, (0,0,90), (5,0), [(1,0), (1,1), (2,1), (3,1), (3,0), (4,0)], [(2,0,'Tree')], "Sequence|ObstacleAvoidance", 20)
make_map(8, (0,0,90), (6,0), [(1,0), (2,0), (2,1), (3,1), (4,1), (4,0), (5,0)], [(3,0,'Stone')], "If|ObstacleAvoidance", 20)
make_map(9, (0,0,90), (4,4), [(1,0), (1,1), (2,1), (2,2), (3,2), (3,3), (4,3)], [], "Function|Spatial", 20)

floors10 = [(i,0) for i in range(1, 10)] + [(1,1), (2,1), (3,1), (5,-1), (6,-1), (7,-1)]
obstacles10 = [(2,0,'Stone'), (6,0,'Stone')]
make_map(10, (0,0,90), (10,0), floors10, obstacles10, "While|If|ObstacleAvoidance", 25)

floors11 = [(1,0),(2,0),(3,0),(3,1),(3,2),(3,3),(4,3),(5,3),(6,3),(6,2),(6,1),(6,0)]
make_map(11, (0,0,90), (7,0), floors11, [], "Function", 25)

# Stage 12 fix (added 4,1 and 7,1)
floors12 = [(1,0),(2,0),(2,1),(3,1),(4,1),(3,0),(4,0),(5,0),(5,1),(6,1),(7,1),(6,0),(7,0)]
obstacles12 = [(3,0,'Box'), (6,0,'Box')]
make_map(12, (0,0,90), (8,0), floors12, obstacles12, "If|ObstacleAvoidance", 25)

floors13 = [(1,0),(2,0),(2,1),(2,2),(1,2),(0,2),(0,1)]
make_map(13, (0,0,90), (0,0), floors13, [], "While|Function", 25)

floors14 = [(1,0),(1,1),(2,1),(2,2),(3,2),(3,3),(4,3),(4,4),(5,4),(5,5)]
make_map(14, (0,0,90), (6,5), floors14, [], "Function", 25)

# Stage 15 fix (added 3,1 / 6,-1 / 9,1)
floors15 = [(1,0),(1,1),(2,1),(3,1),(2,0),(3,0),(4,0),(4,-1),(5,-1),(6,-1),(5,0),(6,0),(7,0),(7,1),(8,1),(9,1),(8,0),(9,0)]
obstacles15 = [(2,0,'Stone'), (5,0,'Stone'), (8,0,'Stone')]
make_map(15, (0,0,90), (10,0), floors15, obstacles15, "While|If", 30)

floors16 = [(1,0),(2,0),(3,0),(4,0),(5,0),(6,0),(2,1),(3,1),(4,1),(5,1),(4,-1),(5,-1),(6,-1)]
obstacles16 = [(3,0,'Tree'), (5,0,'Box')]
make_map(16, (0,0,90), (7,0), floors16, obstacles16, "If|ObstacleAvoidance|Function", 30)

floors17 = [(1,0),(1,1),(1,2),(2,2),(3,2),(3,1),(3,0),(4,0),(5,0),(5,1),(5,2),(6,2),(7,2)]
make_map(17, (0,0,90), (8,2), floors17, [], "Function|If", 30)

floors18 = [(x,z) for x in range(1, 6) for z in range(-2, 3)]
obstacles18 = [(2,0,'Stone'), (3,-1,'Tree'), (4,1,'Box')]
make_map(18, (0,0,90), (6,0), floors18, obstacles18, "While|If|Spatial", 30)

floors19 = [(x,0) for x in range(1,8)] + [(7,z) for z in range(1,8)] + [(x,7) for x in range(6, -1, -1)] + [(0,z) for z in range(6, 1, -1)]
make_map(19, (0,0,90), (0,1), floors19, [], "While|Function|If", 35)

# Stage 20 fix (added alternate routes for all obstacles)
floors20 = [(1,0),(2,0),(2,1),(2,2),(3,2),(4,2),(5,2),(5,1),(5,0),(6,0),(7,0),(7,-1),(7,-2),(8,-2)]
# Avoid for (2,2) -> (3,1)
# Avoid for (5,0) -> (6,1)
# Avoid for (7,-1) -> (8,0), (8,-1)
floors20 += [(3,1), (6,1), (8,0), (8,-1)]
obstacles20 = [(2,2,'Stone'), (5,0,'Tree'), (7,-1,'Box')]
make_map(20, (0,0,90), (9,-2), floors20, obstacles20, "Sequence|Function|If|While|Spatial|ObstacleAvoidance", 45)

print("Generated 20 stages with fixed alternate paths.")
