module Modified

open System
open System.Text
open System.Threading.Tasks

[<Struct>]
type Vec3 =
    { X: float
      Y: float
      Z: float }

let inline vecAdd v1 v2 =
    { X = v1.X + v2.X
      Y = v1.Y + v2.Y
      Z = v1.Z + v2.Z }
let inline vecSub v1 v2 =
    { X = v1.X - v2.X
      Y = v1.Y - v2.Y
      Z = v1.Z - v2.Z }
let inline vecMul v1 v2 =
    { X = v1.X * v2.X
      Y = v1.Y * v2.Y
      Z = v1.Z * v2.Z }
let inline vecDiv v1 v2 =
    { X = v1.X / v2.X
      Y = v1.Y / v2.Y
      Z = v1.Z / v2.Z }

let inline scale s v =
    { X=s*v.X
      Y=s*v.Y
      Z=s*v.Z }

let inline dot v1 v2 =
    let v3 = vecMul v1 v2
    v3.X + v3.Y + v3.Z

let inline norm v =
    dot v v
    |> sqrt

let inline normalise v =
    scale (1.0 / norm v) v

let inline cross v1 v2 =
    { X=v1.Y*v2.Z-v1.Z*v2.Y
      Y=v1.Z*v2.X-v1.X*v2.Z
      Z=v1.X*v2.Y-v1.Y*v2.X }

[<Struct>]
type AABB =
    { Min: Vec3
      Max: Vec3 }

let inline enclosing box0 box1 =
    let small =
        { X = min box0.Min.X box1.Min.X
          Y = min box0.Min.Y box1.Min.Y
          Z = min box0.Min.Z box1.Min.Z }

    let big =
        { X = max box0.Max.X box1.Max.X
          Y = max box0.Max.Y box1.Max.Y
          Z = max box0.Max.Z box1.Max.Z }

    { Min = small; Max = big }

let inline centre aabb =
    { X = aabb.Min.X + aabb.Max.X - aabb.Min.X
      Y = aabb.Min.Y + aabb.Max.Y - aabb.Min.Y
      Z = aabb.Min.Z + aabb.Max.Z - aabb.Min.Z }

type Bvh<'T> =
    | BvhLeaf of AABB * 'T
    | BvhSplit of AABB * Bvh<'T> * Bvh<'T>

let inline BvhAABB bvh =
    match bvh with
    | (BvhLeaf (box, _)) -> box
    | (BvhSplit (box, _, _)) -> box

let inline split n xs =
    match (n, xs) with
    | (0, _) -> struct ([||], xs)
    | (_, [||]) -> struct ([||], [||])
    | (n, xs) -> struct (xs.[..n-1], xs.[n..])

let inline axis d v =
    match d % 3 with
    | 0 -> v.X
    | 1 -> v.Y
    | _ -> v.Z

let mkBvh f allObjs =
    let rec mk d n xs =
        match xs with
        | [||] -> failwith "mk_bvh: no nodes"
        | [| x |] -> BvhLeaf(f x, x)
        | _ ->
            let key x = axis d (centre(f x))
            let xsSorted = Array.sortBy key xs
            let struct (xsLeft, xsRight) = split (n/2) xsSorted
            let xsLeft () = mk (d+1) (n/2) xsLeft
            let doRight () = mk (d+1) (n-(n/2)) xsRight
            let struct (left, right) =
                if n < 100
                then (xsLeft(), doRight())
                else
                    let leftTask =
                        Task.Factory.StartNew(xsLeft,
                                              TaskCreationOptions.None)
                    let rightTask =
                        Task.Factory.StartNew(doRight,
                                              TaskCreationOptions.None)
                    struct (leftTask.Result, rightTask.Result)
            let box = enclosing (BvhAABB left) (BvhAABB right)
            BvhSplit (box, left, right)
    mk 0 (Array.length allObjs) allObjs

type Pos = Vec3
type Dir = Vec3
type Colour = Vec3

let black = { X=0.0; Y=0.0; Z=0.0 }
let white = { X=1.0; Y=1.0; Z=1.0 }

[<Struct>]
type Ray = { Origin: Pos; Dir: Dir }

let pointAtParam ray t =
    vecAdd ray.Origin (scale t ray.Dir)

[<Struct>]
type Hit =
    { T: float
      P: Pos
      Normal: Dir
      Colour: Colour }

[<Struct>]
type Sphere =
    { Pos: Pos
      Colour: Colour
      Radius: float }

let inline sphereAABB s =
    { Min = vecSub s.Pos { X=s.Radius; Y=s.Radius; Z=s.Radius }
      Max = vecAdd s.Pos { X=s.Radius; Y=s.Radius; Z=s.Radius } }

let inline spehreHit s r tMin tMax =
    let oc = vecSub r.Origin s.Pos
    let a = dot r.Dir r.Dir
    let b = dot oc r.Dir
    let c = dot oc oc - s.Radius*s.Radius
    let discriminant = b*b - a*c
    let inline f temp =
        if temp < tMax && temp > tMin then
            let hit =
                { T = temp
                  P = pointAtParam r temp
                  Normal = scale (1.0/s.Radius) (vecSub (pointAtParam r temp) s.Pos)
                  Colour = s.Colour }
            ValueSome hit
        else ValueNone
    if discriminant <= 0.0 then
        ValueNone
    else
        match f ((-b - sqrt(b*b-a*c))/a) with
        | ValueSome hit -> ValueSome hit
        | ValueNone -> f ((-b + sqrt(b*b-a*c))/a)

let inline aabbHit aabb r tmin0 tmax0 =
    let inline iter min' max' origin' dir' tmin' tmax' =
        let invD = 1.0 / dir'
        let t0 = (min' - origin') * invD
        let t1 = (max' - origin') * invD
        let struct (t0', t1') = if invD < 0.0 then struct (t1, t0) else struct (t0, t1)
        let tmin'' = max t0' tmin'
        let tmax'' = min t1' tmax'
        struct (tmin'', tmax'')
    let struct (tmin1, tmax1) =
        iter aabb.Min.X aabb.Max.X r.Origin.X r.Dir.X tmin0 tmax0
    if tmax1 <= tmin1 then
        false
    else
        let struct (tmin2, tmax2) =
            iter aabb.Min.Y aabb.Max.Y r.Origin.Y r.Dir.Y tmin1 tmax1
        if tmax2 <= tmin2 then
            false
        else
            let struct (tmin3, tmax3) =
                iter aabb.Min.Z aabb.Max.Z r.Origin.Z r.Dir.Z tmin2 tmax2
            tmax3 > tmin3

let rec objsHit bvh r tMin tMax =
    match bvh with
    | (BvhLeaf (_, s)) ->
        spehreHit s r tMin tMax
    | (BvhSplit (box, left, right)) ->
        if not (aabbHit box r tMin tMax) then
            ValueNone
        else
            match objsHit left r tMin tMax with
            | ValueSome h ->
                match objsHit right r tMin h.T with
                | ValueNone -> ValueSome h
                | ValueSome h' -> ValueSome h'
            | ValueNone ->
                objsHit right r tMin tMax

[<Struct>]
type Camera =
    { Origin: Pos
      LLC: Pos
      Horizontal: Dir
      Vertical: Dir }

let inline camera lookfrom lookat vup vfov aspect =
    let theta = vfov * Math.PI / 180.0
    let halfHeight = tan (theta / 2.0)
    let halfWidth = aspect * halfHeight
    let origin = lookfrom
    let w = normalise (vecSub lookfrom lookat)
    let u = normalise (cross vup w)
    let v = cross w u
    
    { Origin = lookfrom
      LLC = vecSub
             (vecSub (vecSub origin (scale halfWidth u))
                     (scale halfHeight v)) w
      Horizontal = scale (2.0*halfWidth) u
      Vertical = scale (2.0*halfHeight) v }

let inline getRay cam s t =
    { Origin = cam.Origin
      Dir = vecSub (vecAdd (vecAdd cam.LLC (scale s cam.Horizontal))
                             (scale t cam.Vertical))
                    cam.Origin
    }

let inline reflect v n =
    vecSub v (scale (2.0 * dot v n) n)

let inline scatter r hit =
    let reflected = reflect (normalise r.Dir) hit.Normal
    let scattered = { Origin = hit.P; Dir = reflected }
    
    if dot scattered.Dir hit.Normal > 0.0 then
        ValueSome (scattered, hit.Colour)
    else
        ValueNone

let rec rayColour objs r depth =
    match objsHit objs r 0.001 1000000000.0 with
    | ValueSome hit ->
        match scatter r hit with
        | ValueSome (scattered, attenuation) ->
            if depth < 50 then
                vecMul attenuation (rayColour objs scattered (depth+1))
             else
                black
         | ValueNone -> black
    | ValueNone ->
        let unitDir = normalise r.Dir
        let t = 0.5 * (unitDir.Y + 1.0)
        let bg = { X=0.5; Y=0.7; Z=1.0 }
        vecAdd (scale (1.0-t) white) (scale t bg)

let inline traceRay objs width height cam j i =
    let u = float i / float width
    let v = float j / float height
    let ray = getRay cam u v
    rayColour objs ray 0

let colorToPixel p =
    let ir = int (255.99 * p.X)
    let ig = int (255.99 * p.Y)
    let ib = int (255.99 * p.Z)
    struct (ir, ig, ib)

[<Struct>]
type Image =
    { Pixels: struct(int * int * int) []
      Heigt: int
      Width: int }

let inline image2ppm img =
    let sb = StringBuilder()
    let inline onPixel (struct(r,g,b)) =
        sb.Append(string r + " " +
                  string g + " " +
                  string b + "\n")
    ignore (sb.Append("P3\n" +
                      string img.Width + " " + string img.Heigt + "\n" +
                      "255\n"))
    for pixel in img.Pixels do ignore (onPixel pixel)
    sb.ToString()

let inline render objs width height cam =
    let inline pixel l =
        let i = l % width
        let j = height - l / width
        colorToPixel (traceRay objs width height cam j i)

    let pixels = Array.Parallel.init (height*width) pixel
    
    { Width = width
      Heigt = height
      Pixels = pixels }

[<Struct>]
type Scene =
    { LookFrom: Pos
      LookAt: Pos
      FOV: float
      Spheres: Sphere [] }

let inline fromScene width height scene =
    struct (mkBvh sphereAABB scene.Spheres,
            camera scene.LookFrom scene.LookAt { X=0.0; Y=1.0; Z=0.0 } scene.FOV (float width/float height))

let inline tabulate2D m n f =
    Array.collect (fun j -> Array.map (fun i -> f (j, i)) ([| 0 .. n-1 |])) ([| 0 .. m-1|])

let rgbbox : Scene =
    let n = 10
    let k = 60.0

    let leftwall =
        tabulate2D n n (fun (y, z) ->
                            { Pos={X=(-k/2.0);
                                   Y=(-k/2.0 + (k/float n) * float y);
                                   Z=(-k/2.0 + (k/float n) * float z)}
                              Colour={X=1.0; Y=0.0; Z=0.0}
                              Radius = (k/(float n*2.0))})

    let midwall =
        tabulate2D n n (fun (x,y) ->
                            { Pos={X=(-k/2.0 + (k/float n) * float x);
                                   Y=(-k/2.0 + (k/float n) * float y);
                                   Z=(-k/2.0)}
                              Colour={X=1.0; Y=1.0; Z=0.0}
                              Radius = (k/(float n*2.0))})

    let rightwall =
        tabulate2D n n (fun (y,z) ->
                            { Pos={X=(k/2.0);
                                   Y=(-k/2.0 + (k/float n) * float y);
                                   Z=(-k/2.0 + (k/float n) * float z)}
                              Colour={X=0.0; Y=0.0; Z=1.0}
                              Radius = (k/(float n*2.0))})


    let bottom =
        tabulate2D n n (fun (x,z) ->
                            { Pos={X=(-k/2.0 + (k/float n) * float x);
                                   Y=(-k/2.0);
                                   Z=(-k/2.0 + (k/float n) * float z)}
                              Colour={X=1.0; Y=1.0; Z=1.0}
                              Radius = (k/(float n*2.0))})


    { Spheres =  [| yield! leftwall; yield! midwall; yield! rightwall; yield! bottom |]
      LookFrom = {X=0.0; Y=30.0; Z=30.0}
      LookAt = {X=0.0; Y= -1.0; Z= -1.0}
      FOV = 75.0 }

let irreg : Scene =
    let n = 100
    let k = 600.0
    let bottom =
        tabulate2D n n (fun (x,z) ->
                            { Pos={X=(-k/2.0 + (k/float n) * float x);
                                   Y=0.0;
                                   Z=(-k/2.0 + (k/float n) * float z)}
                              Colour = white
                              Radius = k/(float n * 2.0)})
    { Spheres = bottom
      LookFrom = { X=0.0; Y=12.0; Z=30.0 }
      LookAt = { X=0.0; Y=10.0; Z= -1.0 }
      FOV = 75.0 }

let rec getopt needle argv f def =
    match argv with
    | opt::x::xs ->
        if opt = needle
        then f x else getopt needle (x::xs) f def
    | _ -> def

let inline runScene argv =
    let height = getopt "-m" (Array.toList argv) int 200
    let width = getopt "-n" (Array.toList argv) int 200
    let imgfile = getopt "-f" (Array.toList argv) ValueSome ValueNone
    let sceneName = getopt "-s" (Array.toList argv) id "rgbbox"
    let scene =
        match sceneName with
        | "rgbbox" -> rgbbox
        | "irreg" -> irreg
        | s -> failwith ("No such scene: " + s)

    fromScene width height scene

let inline runRender argv (struct (objs, cam)) =
    let height = getopt "-m" (Array.toList argv) int 200
    let width = getopt "-n" (Array.toList argv) int 200
    let imgfile = getopt "-f" (Array.toList argv) ValueSome ValueNone
    let sceneName = getopt "-s" (Array.toList argv) id "rgbbox"
    let scene =
        match sceneName with
        | "rgbbox" -> rgbbox
        | "irreg" -> irreg
        | s -> failwith ("No such scene: " + s)

    render objs width height cam