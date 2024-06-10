@group(0) @binding(0) var previousLevel: texture_2d<f32>;
@group(0) @binding(1) var nextLevel: texture_storage_2d<rgba8unorm, write>;

@compute @workgroup_size(8, 8)
fn mipmap(@builtin(global_invocation_id) id: vec3<u32>)
{
    let offset = vec2<u32>(0, 1);

    let result =
    (
        textureLoad(previousLevel, 2 * id.xy + offset.xx, 0) +
        textureLoad(previousLevel, 2 * id.xy + offset.xy, 0) +
        textureLoad(previousLevel, 2 * id.xy + offset.yx, 0) +
        textureLoad(previousLevel, 2 * id.xy + offset.yy, 0)
    ) * 0.25f;

    textureStore(nextLevel, id.xy, result);
}