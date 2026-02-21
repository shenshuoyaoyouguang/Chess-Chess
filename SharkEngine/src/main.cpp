#include "uci/UCIProtocol.h"
#include <iostream>
#include <locale>

int main(int argc, char* argv[]) {
    // 设置 UTF-8 编码支持
    #ifdef _WIN32
    std::setlocale(LC_ALL, "zh_CN.UTF-8");
    #endif
    
    SharkEngine::UCIProtocol protocol;
    protocol.run();
    
    return 0;
}
