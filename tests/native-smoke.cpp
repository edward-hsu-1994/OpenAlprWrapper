#include "openalpr_dotnet.h"
#include <fstream>
#include <iostream>
#include <iterator>
#include <vector>

int main(int argc, char** argv) {
    if (argc != 2) {
        std::cerr << "usage: native-smoke <image-path>\n";
        return 2;
    }

    std::ifstream input(argv[1], std::ios::binary);
    if (!input) {
        std::cerr << "cannot open input image\n";
        return 2;
    }
    const std::vector<unsigned char> image(std::istreambuf_iterator<char>(input), {});
    void* engine = alpr_create("us", "", "", 10, 0);
    if (!engine) {
        std::cerr << "OpenALPR engine failed to load\n";
        return 1;
    }

    char* result = alpr_recognize_json(engine, image.data(), image.size());
    if (!result) {
        alpr_destroy(engine);
        std::cerr << "OpenALPR recognition failed\n";
        return 1;
    }
    std::cout << result << '\n';
    alpr_free_string(result);
    alpr_destroy(engine);
    return 0;
}
