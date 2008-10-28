require 'mathcontext'

module Cpg
	class Range
		attr_reader :from, :to, :step, :explicitstep

		def self.normalize(s)
			Range.new(s).to_s
		end
		
		def initialize(s, numdef = 100.0)
			s = s.gsub(/^\s*\[\s*/, '').gsub(/\s*\[\s*$/, '')
			parts = s.split(/\s*[:,]\s*/, 3)
			
			c = MathContext.new
			
			if parts.length == 1
				@from = parts[0]
				@to = parts[0]
				@step = 0
			elsif parts.length == 2
				@from, @to = parts
				@step = (c.eval(@to).to_f - c.eval(@from).to_f) / numdef
			else
				@from, @step, @to = parts
				@explicitstep = true
			end			
		end
		
		def to_s
			(a = []) << @from
			a << @step if @explicitstep
			a << @to
			
			a.join(":")
		end
	end
end
