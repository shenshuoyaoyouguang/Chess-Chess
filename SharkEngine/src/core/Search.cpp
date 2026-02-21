#include "core/Search.h"
#include "core/Evaluation.h"
#include <algorithm>
#include <iostream>

namespace SharkEngine {

Searcher::Searcher() 
    : searching_(false), stopFlag_(false), nodesSearched_(0) {
}

SearchResult Searcher::search(Board& board) {
    result_ = SearchResult{};
    nodesSearched_ = 0;
    stopFlag_ = false;
    startTime_ = std::chrono::steady_clock::now();
    searching_ = true;
    
    int bestScore = -INF_SCORE;
    int alpha = -INF_SCORE;
    int beta = INF_SCORE;
    
    auto legalMoves = board.generateLegalMoves();
    if (legalMoves.empty()) {
        searching_ = false;
        return result_;
    }
    
    sortMoves(legalMoves, board);
    
    Move bestMove;
    
    // 迭代加深搜索（简化版：只搜索固定深度）
    for (int depth = 1; depth <= params_.depth && !shouldStop(); ++depth) {
        result_.depth = depth;
        
        for (const auto& move : legalMoves) {
            if (stopFlag_) break;
            
            Board tempBoard = board;
            if (tempBoard.makeMove(move)) {
                nodesSearched_++;
                
                int score = -alphaBeta(tempBoard, depth - 1, -beta, -alpha, depth == params_.depth);
                
                if (score > bestScore) {
                    bestScore = score;
                    bestMove = move;
                    
                    if (score > alpha) {
                        alpha = score;
                    }
                }
            }
        }
        
        result_.bestMove = bestMove;
        result_.score = bestScore;
        result_.nodes = nodesSearched_;
        result_.timeMs = elapsed();
        
        // 发送当前最佳走法信息
        sendInfo("info depth " + std::to_string(depth) + 
                 " score " + std::to_string(bestScore) +
                 " nodes " + std::to_string(nodesSearched_) +
                 " time " + std::to_string(elapsed()) +
                 " pv " + bestMove.toUCI());
    }
    
    searching_ = false;
    result_.timeMs = elapsed();
    return result_;
}

int Searcher::alphaBeta(Board& board, int depth, int alpha, int beta, bool pvNode) {
    if (shouldStop()) return 0;
    
    // 检查游戏是否结束
    auto gameResult = board.gameResult();
    if (gameResult != GameResult::ONGOING) {
        if (gameResult == GameResult::RED_WIN) {
            return (board.sideToMove() == Color::RED) ? -MATE_SCORE : MATE_SCORE;
        } else if (gameResult == GameResult::BLACK_WIN) {
            return (board.sideToMove() == Color::BLACK) ? -MATE_SCORE : MATE_SCORE;
        }
        return 0; // 和棋
    }
    
    // 叶子节点或达到深度限制
    if (depth == 0) {
        return quiescence(board, alpha, beta);
    }
    
    auto legalMoves = board.generateLegalMoves();
    if (legalMoves.empty()) {
        // 无子可动
        if (board.isInCheck(board.sideToMove())) {
            return -MATE_SCORE; // 将死
        }
        return 0; // 逼和
    }
    
    sortMoves(legalMoves, board);
    
    Move bestMove;
    int bestScore = -INF_SCORE;
    
    for (const auto& move : legalMoves) {
        if (stopFlag_) break;
        
        Board tempBoard = board;
        if (tempBoard.makeMove(move)) {
            nodesSearched_++;
            
            int score = -alphaBeta(tempBoard, depth - 1, -beta, -alpha, pvNode);
            
            if (score > bestScore) {
                bestScore = score;
                bestMove = move;
                
                if (score > alpha) {
                    alpha = score;
                    if (alpha >= beta) {
                        break; // Beta 裁剪
                    }
                }
            }
        }
    }
    
    return bestScore;
}

int Searcher::quiescence(Board& board, int alpha, int beta) {
    int standPat = Evaluation::evaluate(board);
    
    if (standPat >= beta) return beta;
    if (standPat > alpha) alpha = standPat;
    
    // 只搜索吃子走法
    auto captures = MoveGenerator::generateCaptureMoves(board);
    
    for (const auto& move : captures) {
        if (stopFlag_) break;
        
        Board tempBoard = board;
        if (tempBoard.makeMove(move)) {
            nodesSearched_++;
            int score = -quiescence(tempBoard, -beta, -alpha);
            
            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }
    }
    
    return alpha;
}

int Searcher::evaluate(const Board& board) const {
    return Evaluation::evaluate(board);
}

void Searcher::sortMoves(std::vector<Move>& moves, const Board& board) const {
    // 简单排序：吃子走法优先
    std::sort(moves.begin(), moves.end(), [&board](const Move& a, const Move& b) {
        Piece capturedA = board.pieceAt(a.to);
        Piece capturedB = board.pieceAt(b.to);
        Piece movingA = board.pieceAt(a.from);
        Piece movingB = board.pieceAt(b.from);
        
        // MVV-LVA 启发式：最有价值受害者 - 最少价值攻击者
        int valueA = (capturedA.isValid() ? PIECE_VALUES[static_cast<int>(capturedA.type)] * 10 : 0)
                   - (movingA.isValid() ? PIECE_VALUES[static_cast<int>(movingA.type)] : 0);
        int valueB = (capturedB.isValid() ? PIECE_VALUES[static_cast<int>(capturedB.type)] * 10 : 0)
                   - (movingB.isValid() ? PIECE_VALUES[static_cast<int>(movingB.type)] : 0);
        
        return valueA > valueB;
    });
}

bool Searcher::shouldStop() const {
    if (stopFlag_) return true;
    if (params_.nodesLimit > 0 && nodesSearched_ >= params_.nodesLimit) return true;
    if (params_.timeLimit > 0 && elapsed() >= params_.timeLimit) return true;
    return false;
}

void Searcher::sendInfo(const std::string& info) {
    if (infoCallback_) {
        infoCallback_(info);
    }
}

int64_t Searcher::elapsed() const {
    auto now = std::chrono::steady_clock::now();
    return std::chrono::duration_cast<std::chrono::milliseconds>(now - startTime_).count();
}

bool Searcher::timeUp() const {
    return params_.timeLimit > 0 && elapsed() >= params_.timeLimit;
}

void Searcher::startAsync(Board& board) {
    // 异步搜索实现（简化版：同步执行）
    search(board);
}

void Searcher::stop() {
    stopFlag_ = true;
}

} // namespace SharkEngine
