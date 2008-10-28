require 'utils'
require 'graph'

module Cpg
	class GraphSampler
		def initialize(graph, unit_width = 40, dx = 1)
			@graph = graph
			
			@dx = dx
			
			@graph.unit_width = dx
			@graph.sample_frequency = 1

			# timestep for dx pixels
			@dt = dx.to_f / unit_width.to_f
			@sites = []
		end
		
		def add(dt, sample)
			@sites << [(@sites[-1] ? @sites[-1][0] : 0) + dt, sample]
			
			if @sites[-1][0] > @dt
				# resample
				to = (1..(@sites[-1][0] / @dt).floor.to_i).to_a.collect { |x| x * @dt }

				sites = []
				data = []
				@sites.each { |x| sites << x[0]; data << x[1] }
				
				data.resample(sites, to).each do |x|
					@graph << x
				end
				
				# everything smaller than to[-1] can go
				while !@sites.empty? && @sites.first[0] < to[-1]
					@sites.shift
				end
				
				@sites.collect! { |x| [x[0] - to[-1], x[1]] }
			end
		end
	end
end
