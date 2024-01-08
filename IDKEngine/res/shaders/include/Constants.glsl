#ifndef Constants_H
#define Constants_H

#define MATERIAL_EMISSIVE_FACTOR 10.0
#define PI 3.14159265
#define FLOAT_MAX 3.4028235e+38
#define FLOAT_MIN -FLOAT_MAX
#define EPSILON 0.001

// These constants are used in shader and client code. Keep in sync!
#define GPU_MAX_UBO_POINT_SHADOW_COUNT 16
#define GPU_MAX_UBO_LIGHT_COUNT 512

#define TEMPORAL_ANTI_ALIASING_MODE_NO_AA 0
#define TEMPORAL_ANTI_ALIASING_MODE_TAA 1
#define TEMPORAL_ANTI_ALIASING_MODE_FSR2 2

#define MESHLET_MAX_VERTEX_COUNT 128
#define MESHLET_MAX_TRIANGLE_COUNT 252

#endif