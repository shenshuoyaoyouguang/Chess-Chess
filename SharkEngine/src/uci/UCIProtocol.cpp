#include "uci/UCIProtocol.h"
#include <iostream>
#include <sstream>
#include <algorithm>

namespace SharkEngine {

UCIProtocol::UCIProtocol() : searcher_(std::make_unique<Searcher>()) {
    initOptions();
    
    // 设置信息回调
    searcher_->setInfoCallback([this](const std::string& info) {
        send(info);
    });
}

void UCIProtocol::run() {
    std::string line;
    
    while (std::getline(std::cin, line)) {
        if (line.empty()) continue;
        
        // 移除末尾的换行符和空格
        while (!line.empty() && (line.back() == '\r' || line.back() == '\n' || line.back() == ' ')) {
            line.pop_back();
        }
        
        processCommand(line);
        
        if (line == "quit") {
            break;
        }
    }
}

void UCIProtocol::processCommand(const std::string& cmd) {
    std::istringstream iss(cmd);
    std::string token;
    iss >> token;
    
    if (token == "uci") {
        cmdUci();
    } else if (token == "isready") {
        cmdIsReady();
    } else if (token == "ucinewgame") {
        cmdNewGame();
    } else if (token == "position") {
        std::string args;
        std::getline(iss, args);
        while (!args.empty() && args[0] == ' ') args.erase(0, 1);
        cmdPosition(args);
    } else if (token == "go") {
        std::string args;
        std::getline(iss, args);
        while (!args.empty() && args[0] == ' ') args.erase(0, 1);
        cmdGo(args);
    } else if (token == "stop") {
        cmdStop();
    } else if (token == "quit") {
        cmdQuit();
    } else if (token == "setoption") {
        std::string args;
        std::getline(iss, args);
        while (!args.empty() && args[0] == ' ') args.erase(0, 1);
        cmdSetOption(args);
    } else if (token == "ponderhit") {
        cmdPonderHit();
    } else if (token == "d") {
        // Debug: 输出当前棋盘 FEN
        send("info string " + board_.toFEN());
    } else {
        send("info string Unknown command: " + token);
    }
}

void UCIProtocol::cmdUci() {
    send("id name SharkEngine v1.0.0");
    send("id author SharkChess Team");
    
    // 输出选项
    for (const auto& opt : options_) {
        std::ostringstream oss;
        oss << "option name " << opt.name << " type " << opt.type;
        if (opt.type == "spin") {
            oss << " default " << opt.value << " min " << opt.min << " max " << opt.max;
        } else if (opt.type == "check" || opt.type == "button") {
            oss << " default " << opt.value;
        } else if (opt.type == "combo") {
            oss << " default " << opt.value; // TODO: 添加选项列表
        } else if (opt.type == "string") {
            oss << " default " << opt.value;
        }
        send(oss.str());
    }
    
    send("uciok");
}

void UCIProtocol::cmdIsReady() {
    send("readyok");
}

void UCIProtocol::cmdNewGame() {
    board_.reset();
    searcher_ = std::make_unique<Searcher>();
    searcher_->setInfoCallback([this](const std::string& info) {
        send(info);
    });
}

void UCIProtocol::cmdPosition(const std::string& args) {
    std::istringstream iss(args);
    std::string token;
    iss >> token;
    
    if (token == "startpos") {
        board_.reset();
    } else if (token == "fen") {
        std::string fen;
        std::getline(iss, fen);
        // 移除 moves 部分
        size_t movesPos = fen.find(" moves");
        if (movesPos != std::string::npos) {
            fen = fen.substr(0, movesPos);
        }
        board_.fromFEN(fen);
    }
    
    // 处理 moves 部分
    size_t movesPos = args.find("moves");
    if (movesPos != std::string::npos) {
        std::string movesStr = args.substr(movesPos + 6);
        std::istringstream miss(movesStr);
        std::string moveStr;
        
        while (miss >> moveStr) {
            Move move = Move::fromUCI(moveStr);
            if (move.isValid() && board_.isLegalMove(move)) {
                board_.makeMove(move);
            }
        }
    }
}

void UCIProtocol::cmdGo(const std::string& args) {
    SearchParams params = parseGoParams(args);
    
    searcher_->setDepth(params.depth);
    searcher_->setTimeLimit(params.timeLimit);
    searcher_->setNodesLimit(params.nodesLimit);
    
    // 执行搜索
    SearchResult result = searcher_->search(board_);
    
    // 输出最佳走法
    send("bestmove " + result.bestMove.toUCI());
}

void UCIProtocol::cmdStop() {
    searcher_->stop();
}

void UCIProtocol::cmdQuit() {
    // 退出主循环
}

void UCIProtocol::cmdSetOption(const std::string& args) {
    std::istringstream iss(args);
    std::string name, value;
    std::string token;
    
    bool readingName = true;
    while (iss >> token) {
        if (token == "name") {
            readingName = true;
            continue;
        } else if (token == "value") {
            readingName = false;
            continue;
        }
        
        if (readingName) {
            if (!name.empty()) name += " ";
            name += token;
        } else {
            if (!value.empty()) value += " ";
            value += token;
        }
    }
    
    if (!name.empty()) {
        setOptionValue(name, value);
    }
}

void UCIProtocol::cmdPonderHit() {
    // TODO: 实现 ponder 支持
}

void UCIProtocol::cmdDebug(const std::string& args) {
    send("info string Debug: " + args);
}

void UCIProtocol::send(const std::string& msg) {
    if (outputCallback_) {
        outputCallback_(msg);
    } else {
        std::cout << msg << std::endl;
    }
}

SearchParams UCIProtocol::parseGoParams(const std::string& args) {
    SearchParams params;
    
    std::istringstream iss(args);
    std::string token;
    
    while (iss >> token) {
        if (token == "depth") {
            int d;
            if (iss >> d) params.depth = d;
        } else if (token == "movetime") {
            int64_t t;
            if (iss >> t) params.timeLimit = t;
        } else if (token == "wtime") {
            // 红方剩余时间（暂时不使用）
            int64_t t;
            iss >> t;
        } else if (token == "btime") {
            // 黑方剩余时间（暂时不使用）
            int64_t t;
            iss >> t;
        } else if (token == "winc") {
            // 红方加时
            int64_t t;
            iss >> t;
        } else if (token == "binc") {
            // 黑方加时
            int64_t t;
            iss >> t;
        } else if (token == "movestogo") {
            int m;
            iss >> m;
        } else if (token == "infinite") {
            params.infinite = true;
            params.timeLimit = 0; // 无限思考
        } else if (token == "ponder") {
            // Ponder 模式（暂时不支持）
        }
    }
    
    // 默认深度和时间
    if (params.depth == 6 && params.timeLimit == 5000) {
        // 未指定参数时使用默认值
        params.depth = 6;
        params.timeLimit = 5000;
    }
    
    return params;
}

void UCIProtocol::initOptions() {
    options_ = {
        {"Threads", "spin", "1", 1, 128},
        {"Hash", "spin", "16", 1, 2048},
        {"Ponder", "check", "false", 0, 0},
        {"MultiPV", "spin", "1", 1, 500}
    };
}

void UCIProtocol::setOptionValue(const std::string& name, const std::string& value) {
    for (auto& opt : options_) {
        if (opt.name == name) {
            opt.value = value;
            
            // 应用选项
            if (name == "Threads") {
                // TODO: 设置线程数
            } else if (name == "Hash") {
                // TODO: 设置哈希表大小
            } else if (name == "MultiPV") {
                int mpv = std::stoi(value);
                searcher_->setMultiPV(mpv);
            }
            break;
        }
    }
}

} // namespace SharkEngine
