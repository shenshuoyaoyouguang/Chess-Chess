cmake_minimum_required(VERSION 3.16)
project(SharkEngine VERSION 1.0.0 LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# 编译选项
if(MSVC)
    add_compile_options(/W4 /O2 /arch:AVX2)
else()
    add_compile_options(-Wall -O3 -march=native)
endif()

# 源文件
set(ENGINE_SOURCES
    src/core/Board.cpp
    src/core/MoveGen.cpp
    src/core/Search.cpp
    src/core/Evaluation.cpp
    src/uci/UCIProtocol.cpp
    src/main.cpp
)

# 头文件
set(ENGINE_HEADERS
    include/core/Board.h
    include/core/MoveGen.h
    include/core/Search.h
    include/core/Evaluation.h
    include/core/Types.h
    include/uci/UCIProtocol.h
)

# 创建可执行文件
add_executable(sharkengine ${ENGINE_SOURCES} ${ENGINE_HEADERS})

# 包含目录
target_include_directories(sharkengine PRIVATE ${CMAKE_SOURCE_DIR}/include)

# 安装
install(TARGETS sharkengine DESTINATION bin)
