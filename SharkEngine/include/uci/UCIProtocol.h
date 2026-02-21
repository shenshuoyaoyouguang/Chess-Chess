#ifndef SHARK_ENGINE_UCI_PROTOCOL_H
#define SHARK_ENGINE_UCI_PROTOCOL_H

#include "Types.h"
#include "Board.h"
#include "Search.h"
#include <string>
#include <memory>
#include <functional>

namespace SharkEngine {

// UCI 协议处理类
class UCIProtocol {
public:
    UCIProtocol();
    ~UCIProtocol() = default;
    
    // 运行主循环
    void run();
    
    // 设置输出回调
    using OutputCallback = std::function<void(const std::string&)>;
    void setOutputCallback(OutputCallback callback) { outputCallback_ = callback; }

private:
    Board board_;
    std::unique_ptr<Searcher> searcher_;
    OutputCallback outputCallback_;
    
    // 命令处理
    void processCommand(const std::string& cmd);
    
    // UCI 命令
    void cmdUci();
    void cmdIsReady();
    void cmdNewGame();
    void cmdPosition(const std::string& args);
    void cmdGo(const std::string& args);
    void cmdStop();
    void cmdQuit();
    void cmdSetOption(const std::string& args);
    void cmdPonderHit();
    void cmdDebug(const std::string& args);
    
    // 发送输出
    void send(const std::string& msg);
    
    // 解析位置命令
    void parsePosition(const std::string& args);
    
    // 解析 go 参数
    SearchParams parseGoParams(const std::string& args);
    
    // 选项管理
    struct Option {
        std::string name;
        std::string type;
        std::string value;
        int min = 0;
        int max = 0;
    };
    std::vector<Option> options_;
    void initOptions();
    void setOptionValue(const std::string& name, const std::string& value);
};

} // namespace SharkEngine

#endif // SHARK_ENGINE_UCI_PROTOCOL_H
