cmake_minimum_required(VERSION 3.10)
project(openalpr_dotnet LANGUAGES CXX)

find_path(OPENALPR_INCLUDE_DIR NAMES openalpr/alpr.h)
find_library(OPENALPR_LIBRARY NAMES openalpr)
if(NOT OPENALPR_INCLUDE_DIR OR NOT OPENALPR_LIBRARY)
  message(FATAL_ERROR "OpenALPR headers and library are required")
endif()

add_library(openalpr_dotnet SHARED src/openalpr_dotnet.cpp)
target_compile_features(openalpr_dotnet PRIVATE cxx_std_11)
target_include_directories(openalpr_dotnet PRIVATE ${OPENALPR_INCLUDE_DIR})
target_link_libraries(openalpr_dotnet PRIVATE ${OPENALPR_LIBRARY})
set_target_properties(openalpr_dotnet PROPERTIES CXX_VISIBILITY_PRESET hidden VISIBILITY_INLINES_HIDDEN YES)

install(TARGETS openalpr_dotnet LIBRARY DESTINATION lib RUNTIME DESTINATION bin)
