#ifndef SHARK_ENGINE_SEARCH_H
#define SHARK_ENGINE_SEARCH_H

#include "Types.h"
#include "Board.h"
#include <atomic>
#include <chrono>
#include <functional>

namespace SharkEngine {

// 搜索类
class Searcher {
public:
    Searcher();
    ~Searcher() = default;
    
    // 设置搜索参数
    void setDepth(int depth) { params_.depth = depth; }
    void setTimeLimit(int64_t ms) { params_.timeLimit = ms; }
    void setNodesLimit(int64_t nodes) { params_.nodesLimit = nodes; }
    void setMultiPV(int mpv) { params_.multiPV = mpv; }
    
    // 执行搜索
    SearchResult search(Board& board);
    
    // 异步搜索控制
    void startAsync(Board& board);
    void stop();
    bool isSearching() const { return searching_; }
    
    // 回调设置
    using InfoCallback = std::function<void(const std::string&)>;
    void setInfoCallback(InfoCallback callback) { infoCallback_ = callback; }
    
    // 最佳走法查询
    SearchResult getResult() const { return result_; }
    
private:
    SearchParams params_;
    SearchResult result_;
    
    std::atomic<bool> searching_;
    std::atomic<bool> stopFlag_;
    std::atomic<int64_t> nodesSearched_;
    std::chrono::steady_clock::time_point startTime_;
    
    InfoCallback infoCallback_;
    
    // Alpha-Beta 搜索核心
    int alphaBeta(Board& board, int depth, int alpha, int beta, bool pvNode);
    
    // 静态搜索
    int quiescence(Board& board, int alpha, int beta);
    
    // 评估函数（转发到Evaluation）
    int evaluate(const Board& board) const;
    
    // 排序走法
    void sortMoves(std::vector<Move>& moves, const Board& board) const;
    
    // 检查是否应该停止搜索
    bool shouldStop() const;
    
    // 发送信息
    void sendInfo(const std::string& info);
    
    // 时间管理
    int64_t elapsed() const;
    bool timeUp() const;
};

} // namespace SharkEngine

#endif // SHARK_ENGINE_SEARCH_H
