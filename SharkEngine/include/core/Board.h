#ifndef SHARK_ENGINE_BOARD_H
#define SHARK_ENGINE_BOARD_H

#include "Types.h"
#include <array>
#include <vector>
#include <string>
#include <functional>

namespace SharkEngine {

// 棋盘类
class Board {
public:
    Board();
    ~Board() = default;
    
    // 初始化
    void reset();
    
    // 棋子访问
    Piece pieceAt(Square sq) const;
    void setPiece(Square sq, Piece piece);
    void removePiece(Square sq);
    
    // 走棋
    bool makeMove(const Move& move);
    void unmakeMove(const Move& move);
    
    // 合法性检查
    bool isLegalMove(const Move& move) const;
    bool isInCheck(Color color) const;
    bool isMoveLegal(const Move& move) const;
    
    // 生成所有合法走法
    std::vector<Move> generateLegalMoves() const;
    
    // 游戏状态
    Color sideToMove() const { return sideToMove_; }
    GameResult gameResult() const;
    
    // FEN 支持
    std::string toFEN() const;
    bool fromFEN(const std::string& fen);
    
    // Zobrist 哈希
    uint64_t hash() const { return hash_; }
    
    // 走棋历史
    const std::vector<Move>& moveHistory() const { return moveHistory_; }
    int halfMoveClock() const { return halfMoveClock_; }
    int fullMoveNumber() const { return fullMoveNumber_; }
    
    // 棋盘位置有效性
    static bool isValidSquare(Square sq);
    
    // 九宫格区域检查
    static bool isInPalace(Square sq, Color color);
    
    // 河界检查
    static bool isAcrossRiver(Square sq, Color color);
    
private:
    // 棋盘表示：90格数组
    std::array<Piece, 90> board_;
    
    // 游戏状态
    Color sideToMove_;
    int halfMoveClock_;   // 无吃子半回合计数
    int fullMoveNumber_;  // 全回合计数
    
    // Zobrist 哈希
    uint64_t hash_;
    
    // 走棋历史
    std::vector<Move> moveHistory_;
    
    // 初始化标准开局
    void setupInitialPosition();
    
    // Zobrist 相关
    void updateHash(Square sq, Piece piece);
    static uint64_t pieceHashKey(Color color, PieceType type, Square sq);
    static uint64_t sideHashKey();
    
    // 走棋生成辅助
    void generatePawnMoves(Square from, std::vector<Move>& moves) const;
    void generateKnightMoves(Square from, std::vector<Move>& moves) const;
    void generateBishopMoves(Square from, std::vector<Move>& moves) const;
    void generateRookMoves(Square from, std::vector<Move>& moves) const;
    void generateCannonMoves(Square from, std::vector<Move>& moves) const;
    void generateAdvisorMoves(Square from, std::vector<Move>& moves) const;
    void generateKingMoves(Square from, std::vector<Move>& moves) const;
    
    // 走法合理性检查
    bool isPawnMoveLegal(Square from, Square to, Color color) const;
    bool isKnightMoveLegal(Square from, Square to) const;
    bool isBishopMoveLegal(Square from, Square to, Color color) const;
    bool isAdvisorMoveLegal(Square from, Square to, Color color) const;
    bool isKingMoveLegal(Square from, Square to, Color color) const;
    bool canCastleKingMove(Square from, Square to) const; // 将帅对脸检测
};

} // namespace SharkEngine

#endif // SHARK_ENGINE_BOARD_H
