#ifndef SHARK_ENGINE_TYPES_H
#define SHARK_ENGINE_TYPES_H

#include <cstdint>
#include <string>
#include <array>

namespace SharkEngine {

// 棋子类型枚举
enum class PieceType : int8_t {
    NONE = 0,
    KING = 1,   // 将/帅
    ADVISOR,    // 士/仕
    BISHOP,     // 象/相
    KNIGHT,     // 马
    ROOK,       // 车
    CANNON,     // 炮
    PAWN        // 兵/卒
};

// 颜色枚举
enum class Color : int8_t {
    RED = 0,    // 红方
    BLACK = 1,  // 黑方
    NONE = 2
};

// 棋子结构
struct Piece {
    PieceType type = PieceType::NONE;
    Color color = Color::NONE;
    
    bool isEmpty() const { return type == PieceType::NONE; }
    bool isValid() const { return type != PieceType::NONE && color != Color::NONE; }
    
    // 获取棋子显示字符
    std::wstring getSymbol() const {
        static const std::wstring redPieces[] = {L"", L"帅", L"仕", L"相", L"马", L"车", L"炮", L"兵"};
        static const std::wstring blackPieces[] = {L"", L"将", L"士", L"象", L"马", L"车", L"炮", L"卒"};
        
        if (color == Color::RED) {
            return redPieces[static_cast<int>(type)];
        } else if (color == Color::BLACK) {
            return blackPieces[static_cast<int>(type)];
        }
        return L"";
    }
};

// 位置坐标 (文件=纵线1-9, 等级=横线1-10)
struct Square {
    int8_t file;   // 1-9 (红方视角)
    int8_t rank;   // 1-10
    
    Square() : file(0), rank(0) {}
    Square(int f, int r) : file(f), rank(r) {}
    
    bool isValid() const { return file >= 1 && file <= 9 && rank >= 1 && rank <= 10; }
    
    // 转换为数组索引 (0-89)
    int toIndex() const { return (rank - 1) * 9 + (file - 1); }
    
    // 从索引创建
    static Square fromIndex(int index) {
        return Square(index % 9 + 1, index / 9 + 1);
    }
    
    bool operator==(const Square& other) const {
        return file == other.file && rank == other.rank;
    }
};

// 走法结构
struct Move {
    Square from;
    Square to;
    Piece captured;      // 被吃的棋子
    PieceType promotion; // 升变（中国象棋无升变，预留）
    
    Move() : promotion(PieceType::NONE) {}
    Move(Square f, Square t) : from(f), to(t), promotion(PieceType::NONE) {}
    
    // 转换为 UCI 字符串 (如 "a0a1")
    std::string toUCI() const {
        // 中国象棋使用数字坐标：文件1-9，等级0-9(黑方底线为0)
        char fromFile = 'a' + from.file - 1;
        char fromRank = '0' + (10 - from.rank);
        char toFile = 'a' + to.file - 1;
        char toRank = '0' + (10 - to.rank);
        return std::string{fromFile, fromRank, toFile, toRank};
    }
    
    // 从 UCI 字符串解析
    static Move fromUCI(const std::string& uci) {
        if (uci.length() < 4) return Move();
        
        int fromFile = uci[0] - 'a' + 1;
        int fromRank = 10 - (uci[1] - '0');
        int toFile = uci[2] - 'a' + 1;
        int toRank = 10 - (uci[3] - '0');
        
        return Move(Square(fromFile, fromRank), Square(toFile, toRank));
    }
    
    bool isValid() const { return from.isValid() && to.isValid(); }
};

// 游戏状态
enum class GameResult : int8_t {
    ONGOING = 0,
    RED_WIN = 1,
    BLACK_WIN = 2,
    DRAW = 3
};

// 搜索参数
struct SearchParams {
    int depth = 6;              // 搜索深度
    int64_t timeLimit = 5000;   // 时间限制(ms)
    int64_t nodesLimit = 0;     // 节点限制(0为无限制)
    bool infinite = false;      // 无限思考
    int multiPV = 1;            // 多主变例数量
};

// 搜索结果
struct SearchResult {
    Move bestMove;
    int score = 0;              // 厘兵值（红方视角）
    int depth = 0;              // 实际搜索深度
    int64_t nodes = 0;          // 搜索节点数
    int64_t timeMs = 0;         // 用时(毫秒)
    std::string pv;             // 主变例
};

// 常量
constexpr int INF_SCORE = 100000;
constexpr int MATE_SCORE = 99000;

} // namespace SharkEngine

#endif // SHARK_ENGINE_TYPES_H
