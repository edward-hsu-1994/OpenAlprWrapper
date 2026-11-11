#include <openalpr/alpr.h>
#include <cstdlib>
#include <cstring>
#include <exception>
#include <string>
#include <vector>

#if defined(_WIN32)
#define ALPR_API extern "C" __declspec(dllexport)
#else
#define ALPR_API extern "C" __attribute__((visibility("default")))
#endif

struct Engine { alpr::Alpr value; Engine(const std::string& country, const std::string& config, const std::string& runtime)
    : value(country, config, runtime) {} };

ALPR_API void* alpr_create(const char* country, const char* config, const char* runtime, int topn, int detect_region) {
    try {
        auto* engine = new Engine(country ? country : "us", config ? config : "", runtime ? runtime : "");
        engine->value.setTopN(topn);
        engine->value.setDetectRegion(detect_region != 0);
        if (!engine->value.isLoaded()) { delete engine; return nullptr; }
        return engine;
    } catch (...) { return nullptr; }
}

ALPR_API char* alpr_recognize_json(void* opaque, const unsigned char* bytes, size_t length) {
    if (!opaque || !bytes || length == 0) return nullptr;
    try {
        auto* engine = static_cast<Engine*>(opaque);
        std::vector<char> image(bytes, bytes + length);
        const auto result = engine->value.recognize(image);
        const auto json = alpr::Alpr::toJson(result);
        auto* copy = static_cast<char*>(std::malloc(json.size() + 1));
        if (!copy) return nullptr;
        std::memcpy(copy, json.c_str(), json.size() + 1);
        return copy;
    } catch (...) { return nullptr; }
}

ALPR_API void alpr_free_string(char* value) { std::free(value); }
ALPR_API void alpr_destroy(void* opaque) { delete static_cast<Engine*>(opaque); }
