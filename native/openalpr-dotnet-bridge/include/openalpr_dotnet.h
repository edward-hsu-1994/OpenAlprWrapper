#pragma once

#include <stddef.h>

#if defined(_WIN32)
#define ALPR_DOTNET_API __declspec(dllexport)
#else
#define ALPR_DOTNET_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

ALPR_DOTNET_API void* alpr_create(const char* country, const char* config, const char* runtime, int topn, int detect_region);
ALPR_DOTNET_API char* alpr_recognize_json(void* engine, const unsigned char* bytes, size_t length);
ALPR_DOTNET_API void alpr_free_string(char* value);
ALPR_DOTNET_API void alpr_destroy(void* engine);

#ifdef __cplusplus
}
#endif
